using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace LibSaberPatch
{
    public class SerializedAssets
    {
        /// padding between metadata and data
        int paddingLen;

        string version;
        int targetPlatform;
        bool enableTypeTree;

        public List<SerializedAssets.TypeRef> types;
        public List<SerializedAssets.AssetObject> objects;
        public List<SerializedAssets.Script> scripts;
        public List<SerializedAssets.External> externals;

        public class TypeRef {
            public int classID;
            bool isStripped;
            ushort scriptTypeIndex;
            byte[] scriptID;
            byte[] typeHash;

            public TypeRef(BinaryReader reader) {
                classID = reader.ReadInt32();
                isStripped = reader.ReadBoolean();
                scriptTypeIndex = reader.ReadUInt16();
                if(classID == 114) {
                    scriptID = reader.ReadBytes(16);
                }
                typeHash = reader.ReadBytes(16);
            }

            public void WriteTo(BinaryWriter w) {
                w.Write(classID);
                w.Write(isStripped);
                w.Write(scriptTypeIndex);
                if(classID == 114) {
                    w.Write(scriptID);
                }
                w.Write(typeHash);
            }
        }

        public class AssetObject {
            public ulong pathID;
            public int typeID;
            public AssetData data;
            public int paddingLen;

            // These are only used during read in and write out. Between those
            // times modifications can make them invalid but they'll be fixed
            // up during the Write process.
            public int offset;
            public int size;


            public AssetObject(BinaryReader reader) {
                reader.AlignStream();
                pathID = reader.ReadUInt64();
                offset = reader.ReadInt32();
                size = reader.ReadInt32();
                typeID = reader.ReadInt32();
                // Console.WriteLine((pathID, offset, size, typeID));
            }

            public void WriteTo(BinaryWriter w) {
                w.AlignStream();
                w.Write(pathID);
                w.Write(offset);
                w.Write(size);
                w.Write(typeID);
            }
        }

        public class Script {
            int fileIndex;
            ulong inFileID;

            public Script(BinaryReader reader) {
                fileIndex = reader.ReadInt32();
                reader.AlignStream();
                inFileID = reader.ReadUInt64();
            }

            public void WriteTo(BinaryWriter w) {
                w.Write(fileIndex);
                w.AlignStream();
                w.Write(inFileID);
            }
        }

        public class External {
            string tempEmpty;
            byte[] guid;
            int type;
            string pathName;

            public External(BinaryReader reader) {
                tempEmpty = reader.ReadStringToNull();
                guid = reader.ReadBytes(16);
                type = reader.ReadInt32();
                pathName = reader.ReadStringToNull();
            }

            public void WriteTo(BinaryWriter w) {
                w.WriteCString(tempEmpty);
                w.Write(guid);
                w.Write(type);
                w.WriteCString(pathName);
            }
        }

        public class ParseException : ApplicationException {
            public ParseException(string msg) : base(msg) {}
        }

        public static byte[] JoinedContents(string basePath) {
            using (MemoryStream stream = new MemoryStream()) {
                string splitBase = basePath + ".split";
                for(int i = 0; File.Exists(splitBase + i); i++) {
                    using (Stream fileStream = new FileStream(splitBase+i, FileMode.Open)) {
                        fileStream.CopyTo(stream);
                    }
                }
                stream.Close();
                return stream.ToArray();
            }
        }

        private const int headerLen = 5*4;
        private const int parsedGeneration = 17;

        public SerializedAssets(Stream stream) {
            BinaryReader reader = new BinaryReader(stream);

            // ===== Parse Header
            int metadataSize = reader.ReadInt32BE();
            int fileSize = reader.ReadInt32BE();
            int generation = reader.ReadInt32BE();
            if(generation != parsedGeneration) throw new ParseException("Unsupported format version");
            int dataOffset = reader.ReadInt32BE();
            int isBigEndian = reader.ReadInt32BE();
            if(isBigEndian != 0) throw new ParseException("Must be little endian");
            paddingLen = dataOffset - (metadataSize + headerLen);

            // ===== Parse Metadata
            version = reader.ReadStringToNull();
            if(version != "2018.3.10f1") throw new ParseException("Unsupported Unity version");
            targetPlatform = reader.ReadInt32();
            enableTypeTree = reader.ReadBoolean();
            if(enableTypeTree) throw new ParseException("Type trees aren't supported");

            types = reader.ReadPrefixedList(r => new TypeRef(r));
            objects = reader.ReadPrefixedList(r => new AssetObject(r));
            scripts = reader.ReadPrefixedList(r => new Script(r));
            externals = reader.ReadPrefixedList(r => new External(r));
            // this is necessary to get headerLen+metadataSize to match up with offset
            if(reader.ReadByte() != 0) throw new ParseException("Expected metadata to end with 0");

            if(!reader.ReadAllZeros(paddingLen)) throw new ParseException("Expected zeros for padding");
            Debug.Assert(reader.BaseStream.Position == dataOffset, "Parsed metadata wrong");

            // ===== Parse Data
            for(int i = 0; i < objects.Count-1; i++) {
                objects[i].paddingLen = objects[i+1].offset-(objects[i].offset+objects[i].size);
            }
            // I've never seen any padding after the last object but handle it just in case
            AssetObject last = objects[objects.Count-1];
            int dataSize = fileSize - dataOffset;
            last.paddingLen = dataSize - (last.offset+last.size);

            foreach(AssetObject obj in objects) {
                // Console.WriteLine((reader.BaseStream.Position-dataOffset, obj.offset, obj.size));
                if(reader.BaseStream.Position-dataOffset != obj.offset) {
                    throw new ParseException("Objects aren't in order");
                }
                obj.data = new UnknownAssetData(reader.ReadBytes(obj.size));
                if(!reader.ReadAllZeros(obj.paddingLen)) throw new ParseException("Expected zeros for padding");
            }
        }

        public void WriteTo(Stream outStream) {
            // We write the sections in reverse order because each section needs sizes/offsets
            // from the previous sections.

            // ===== Write Data
            byte[] data;
            using (MemoryStream stream = new MemoryStream()) {
                BinaryWriter w = new BinaryWriter(stream);

                foreach(AssetObject obj in objects) {
                    obj.offset = (int)w.BaseStream.Position;
                    obj.data.WriteTo(w);
                    obj.size = (int)w.BaseStream.Position - obj.offset;
                    w.WriteZeros(obj.paddingLen);

                    // TODO do objects need to be aligned?
                    // All objects I can find are. If nothing is modified this shouldn't do anything.
                    // But if we change the size of an object it's probably more important to preserve
                    // alignment than the exact amount of padding.
                    w.AlignStream();
                }

                stream.Close();
                data = stream.ToArray();
            }

            // ===== Write Metadata
            byte[] metadata;
            using (MemoryStream stream = new MemoryStream()) {
                BinaryWriter w = new BinaryWriter(stream);

                w.WriteCString(version);
                w.Write(targetPlatform);
                w.Write(enableTypeTree);

                w.WritePrefixedList(types, x => x.WriteTo(w));
                w.WritePrefixedList(objects, x => x.WriteTo(w));
                w.WritePrefixedList(scripts, x => x.WriteTo(w));
                w.WritePrefixedList(externals, x => x.WriteTo(w));
                w.Write((byte)0);

                stream.Close();
                metadata = stream.ToArray();
            }


            // ===== Write Header and final content
            {
                BinaryWriter w = new BinaryWriter(outStream);
                int dataOffset = headerLen + metadata.Length + paddingLen;
                int fileSize = dataOffset + data.Length;
                w.WriteInt32BE(metadata.Length);
                w.WriteInt32BE(fileSize);
                w.WriteInt32BE(parsedGeneration);
                w.WriteInt32BE(dataOffset);
                w.WriteInt32BE(0); // not big endian

                w.Write(metadata);
                w.WriteZeros(paddingLen);
                // TODO align in case of modification changing metadata size
                w.Write(data);
            }
        }
    }
}

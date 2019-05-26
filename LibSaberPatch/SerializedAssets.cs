using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace LibSaberPatch
{
    public class SerializedAssets
    {
        /// padding between metadata and data
        public int paddingLen;

        public string version;
        public int targetPlatform;
        public bool enableTypeTree;

        public List<SerializedAssets.TypeRef> types;

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
        }

        public SerializedAssets(Stream stream) {
            BinaryReader reader = new BinaryReader(stream);

            // ===== Parse Header
            int metadataSize = reader.ReadInt32BE();
            int fileSize = reader.ReadInt32BE();
            int generation = reader.ReadInt32BE();
            if(generation != 17) throw new ParseException("Unsupported format version");
            int dataOffset = reader.ReadInt32BE();
            int isBigEndian = reader.ReadInt32BE();
            if(isBigEndian != 0) throw new ParseException("Must be little endian");
            paddingLen = dataOffset - (metadataSize + 5*4);

            // ===== Parse Metadata
            version = reader.ReadStringToNull();
            if(version != "2018.3.10f1") throw new ParseException("Unsupported Unity version");
            targetPlatform = reader.ReadInt32();
            enableTypeTree = reader.ReadBoolean();

            types = reader.ReadPrefixedList(r => new TypeRef(r));
        }
    }
}

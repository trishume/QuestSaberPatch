﻿using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LibSaberPatch.BehaviorDataObjects;
using LibSaberPatch.AssetDataObjects;

namespace LibSaberPatch
{
    public sealed class SerializedAssets
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

        // ===== Extra fields, these aren't in the binary but are useful
        public Dictionary<byte[],AssetPtr> scriptIDToScriptPtr;
        public Dictionary<string,AssetPtr> environmentIDToPtr;

        public sealed class TypeRef {
            public int classID;
            public bool isStripped;
            public ushort scriptTypeIndex;
            public byte[] scriptID;
            public byte[] typeHash;

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

        public sealed class AssetObject {
            public ulong pathID;
            public int typeID;
            public AssetData data;
            public int paddingLen;

            // These are only used during read in and write out. Between those
            // times modifications can make them invalid but they'll be fixed
            // up during the Write process.
            public int offset;
            public int size;

            public AssetObject() {}

            public AssetObject(BinaryReader reader) {
                reader.AlignStream();
                pathID = reader.ReadUInt64();
                offset = reader.ReadInt32();
                size = reader.ReadInt32();
                typeID = reader.ReadInt32();
            }

            // returns the location to patch the offsets
            public long WriteTo(BinaryWriter w) {
                w.AlignStream();
                w.Write(pathID);
                long patchPos = w.BaseStream.Position;
                w.Write(offset);
                w.Write(size);
                w.Write(typeID);
                return patchPos;
            }
        }

        public sealed class Script {
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

        public sealed class External {
            public string tempEmpty;
            public byte[] guid;
            public int type;
            public string pathName;

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

        public sealed class ParseException : ApplicationException {
            public ParseException(string msg) : base(msg) {}
        }

        private const int headerLen = 5*4;
        private const int parsedGeneration = 17;

        public static SerializedAssets FromBytes(byte[] data) {
            using (Stream stream = new MemoryStream(data)) {
                return new SerializedAssets(stream);
            }
        }

        public byte[] ToBytes() {
            using (MemoryStream stream = new MemoryStream()) {
                WriteTo(stream);
                stream.Close();
                return stream.ToArray();
            }
        }

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

            // ===== Extra stuff
            scriptIDToScriptPtr = new Dictionary<byte[], AssetPtr>(new ByteArrayComparer());
            environmentIDToPtr = new Dictionary<string, AssetPtr>();

            // ===== Parse Data
            for(int i = 0; i < objects.Count-1; i++) {
                objects[i].paddingLen = objects[i+1].offset-(objects[i].offset+objects[i].size);
            }
            // I've never seen any padding after the last object but handle it just in case
            AssetObject last = objects[objects.Count-1];
            int dataSize = fileSize - dataOffset;
            last.paddingLen = dataSize - (last.offset+last.size);

            for(int idx = 0; idx < objects.Count; ++idx) {
                var obj = objects[idx];

                // Console.WriteLine((reader.BaseStream.Position-dataOffset, obj.offset, obj.size));
                if(reader.BaseStream.Position-dataOffset != obj.offset) {
                    throw new ParseException("Objects aren't in order");
                }
                long startOffset = reader.BaseStream.Position;
                if (types.Count <= obj.typeID) {
                    // Console.WriteLine($"TypeID of SimpleColor: {objects[52].typeID} with type: {objects[52].data.GetType()}");
                    throw new ParseException($"Could not find typeID: {obj.typeID}, maximum is: {types.Count}");
                }
                switch(types[obj.typeID].classID) {
                    case MonoBehaviorAssetData.ClassID:
                        byte[] scriptID = types[obj.typeID].scriptID;
                        var monob = new MonoBehaviorAssetData(reader, obj.size, scriptID);
                        scriptIDToScriptPtr[scriptID] = monob.script;
                        obj.data = monob;
                        break;
                    case AudioClipAssetData.ClassID:
                        obj.data = new AudioClipAssetData(reader, obj.size);
                        break;
                    case Texture2DAssetData.ClassID:
                        obj.data = new Texture2DAssetData(reader, obj.size);
                        break;
                    case SpriteAssetData.ClassID:
                        obj.data = new SpriteAssetData(reader, obj.size);
                        break;
                    case GameObjectAssetData.ClassID:
                        obj.data = new GameObjectAssetData(reader, obj.size);
                        break;
                    case MeshFilterAssetData.ClassID:
                        obj.data = new MeshFilterAssetData(reader, obj.size);
                        break;
                    case TextAssetData.ClassID:
                        obj.data = new TextAssetData(reader, obj.size);
                        break;
                    default:
                        obj.data = new UnknownAssetData(reader, obj.size);
                        break;
                }
                long bytesParsed = reader.BaseStream.Position - startOffset;
                if(bytesParsed != obj.size)
                    throw new ParseException($"Parsed {bytesParsed} bytes but expected {obj.size} for path ID {obj.pathID}");
                if(!reader.ReadAllZeros(obj.paddingLen)) throw new ParseException("Expected zeros for padding");
            }

            FindEnvironmentPointers();
        }

        private static void PatchInt<T>(T stream, long index, int val, bool bigEndian, bool preserve = true) where T : Stream {
            byte[] buff = BitConverter.GetBytes(val);
            if(bigEndian) Array.Reverse(buff);
            var prev = stream.Position;
            stream.Position = index;
            stream.Write(buff, 0, buff.Length);
            if (preserve) stream.Position = prev;
        }

        public void WriteTo<T>(T unbufferedStream) where T : Stream {
            using (var stream = new BufferedStream(unbufferedStream)) {
                List<long> patchLocs = new List<long>(objects.Count);
                int dataOffset;
                int metadataSize;

                BinaryWriter w = new BinaryWriter(stream);

                // ===== Header
                w.Write((int)0); // patch
                w.Write((int)0); // patch
                w.WriteInt32BE(parsedGeneration);
                w.Write((int)0); // patch
                w.WriteInt32BE(0); // not big endian

                // ===== Metadata
                w.WriteCString(version);
                w.Write(targetPlatform);
                w.Write(enableTypeTree);

                w.WritePrefixedList(types, x => x.WriteTo(w));
                w.WritePrefixedList(objects, x => patchLocs.Add(x.WriteTo(w)));
                w.WritePrefixedList(scripts, x => x.WriteTo(w));
                w.WritePrefixedList(externals, x => x.WriteTo(w));
                w.Write((byte)0);
                metadataSize = (int)w.BaseStream.Position - headerLen;

                w.WriteZeros(paddingLen);
                w.AlignStream();

                // ===== Data
                dataOffset = (int)w.BaseStream.Position;

                var serializedObjects = new byte[objects.Count][];
                Parallel.ForEach(objects, (obj, state, idx) => {
                    using(var objMemStream = new MemoryStream())
                    using(var objStream = new BufferedStream(objMemStream)) {
                        obj.data.WriteTo(new BinaryWriter(objStream));
                        obj.size = (int)objStream.Length;
                        objStream.Flush();
                        serializedObjects[idx] = objMemStream.GetBuffer();
                    }
                });

                for(int idx = 0; idx < objects.Count; ++idx) {
                    objects[idx].offset = (int)w.BaseStream.Position - dataOffset;
                    w.Write(serializedObjects[idx], 0, objects[idx].size);
                    w.WriteZeros(objects[idx].paddingLen);

                    // TODO do objects need to be aligned?
                    // All objects I can find are. If nothing is modified this shouldn't do anything.
                    // But if we change the size of an object it's probably more important to preserve
                    // alignment than the exact amount of padding.
                    w.AlignStream();
                }

                // Patch header
                PatchInt(stream, 0*4, metadataSize, true, true);
                PatchInt(stream, 1*4, (int)stream.Position, true, false);
                PatchInt(stream, 3*4, dataOffset, true, false);

                // Patch objects
                for(int i = 0; i < patchLocs.Count; i++) {
                    PatchInt(stream, patchLocs[i] + 0*4, objects[i].offset, false, false);
                    PatchInt(stream, patchLocs[i] + 1*4, objects[i].size, false, false);
                }

                stream.Flush();
            }
        }

        public AssetPtr AppendAsset(AssetData data) {
            ulong pathID = (ulong)(objects.Count + 1);
            AssetObject obj = new AssetObject() {
                pathID = pathID,
                typeID = data.SharedAssetsTypeIndex(),
                data = data,
                paddingLen = 0,
            };
            objects.Add(obj);
            return new AssetPtr(0, pathID);
        }

        private void ShiftPathIDs(int startIndex, ulong delta, ulong startPathID)
        {
            // Shift all remaining PathIDs by delta
            Action<AssetPtr> shift = null;
            shift = (ptr) =>
            {
                if (ptr.fileID == 0 && ptr.pathID > startPathID) {
                    ptr.pathID -= delta;
                    var ast = GetAssetAt(ptr.pathID);
                    if (ast == null) throw new ApplicationException($"Could not find PathID: {ptr.pathID}");
                    ast.data.Trace(shift);
                }
            };

            for (int i = startIndex; i < objects.Count; i++)
            {
                objects[i].data.Trace(shift);
                if (objects[i].pathID > startPathID)
                {
                    objects[i].pathID -= delta;
                }
            }
        }

        public AssetObject GetAsset(Predicate<AssetObject> p)
        {
            return objects.Find(p);
        }

        public AssetObject GetAssetObjectFromScript<T>(Predicate<T> cond) where T : BehaviorData
        {
            return GetAssetObjectFromScript<T>(mob => true, cond);
        }

        public AssetObject GetAssetObjectFromScript<T>(Predicate<MonoBehaviorAssetData> mob, Predicate<T> cond) where T : BehaviorData
        {
            foreach(AssetObject obj in objects) {
                if(!(obj.data is MonoBehaviorAssetData)) continue;
                MonoBehaviorAssetData monob = (MonoBehaviorAssetData)obj.data;
                if(!(monob.data is T) || !mob(monob)) continue;
                T behaviorData = (T)monob.data;
                if(cond(behaviorData)) return obj;
            }
            return null;
        }

        // This should only be called with objects after the base game assets
        // that are not referenced by any pointers.
        public AssetObject RemoveAsset(Predicate<AssetObject> p) {
            // First, find matching AssetObj
            int objI = objects.FindIndex(p);
            // Console.WriteLine($"{objects[objI].pathID} has type: {objects[objI].data.GetType()}");
            AssetObject obj = objects[objI];
            ShiftPathIDs(objI, 1, obj.pathID);
            objects.RemoveAt(objI);
            return obj;
        }

        public AssetObject RemoveAssetAt(ulong pathID)
        {
            return RemoveAsset(d => d.pathID == pathID);
        }

        public AssetObject RemoveScript(BehaviorData data)
        {
            return RemoveAsset(ao => ao.data.GetType().Equals(typeof(MonoBehaviorAssetData))
            && (ao.data as MonoBehaviorAssetData).data.Equals(data));
        }

        public AssetObject GetAssetAt(ulong pathID)
        {
            return objects.Find(d => d.pathID == pathID);
        }

        public AssetObject SetAssetAt(ulong pathID, AssetData data)
        {
            int ind = objects.FindIndex(d => d.pathID == pathID);
            objects[ind] = new AssetObject()
            {
                pathID = pathID,
                typeID = data.SharedAssetsTypeIndex(),
                data = data,
                paddingLen = 0,
            };
            // Shift offsets of all other objects by the delta size of this,
            // should get taken care of automatically.
            return objects[ind];
        }

        public LevelBehaviorData GetLevelMatching(string levelID)
        {
            return FindScript<LevelBehaviorData>(p => p.levelID == levelID);
        }

        public HashSet<string> ExistingLevelIDs() {
            var set = new HashSet<string>();
            foreach(AssetObject obj in objects) {
                if(!(obj.data is MonoBehaviorAssetData))
                    continue;
                MonoBehaviorAssetData monob = (MonoBehaviorAssetData)obj.data;
                if(!(monob.data is LevelBehaviorData))
                    continue;
                LevelBehaviorData levelData = (LevelBehaviorData)monob.data;
                set.Add(levelData.levelID);
            }
            return set;
        }

        public Dictionary<string, ulong> FindLevels() {
            var dict = new Dictionary<string, ulong>();
            foreach(AssetObject obj in objects) {
                if(!(obj.data is MonoBehaviorAssetData))
                    continue;
                MonoBehaviorAssetData monob = (MonoBehaviorAssetData)obj.data;
                if(!(monob.data is LevelBehaviorData))
                    continue;
                LevelBehaviorData levelData = (LevelBehaviorData)monob.data;
                dict.Add(levelData.levelID, obj.pathID);
            }
            return dict;
        }

        public ulong MainAssetsMaxBaseGamePath() {
            var lastBaseObject = objects.Find(o => (o.data is MonoBehaviorAssetData) &&
                (o.data as MonoBehaviorAssetData).name == "SimpleRetailDemoMenuScenesTransitionSetupDataSO");
            return lastBaseObject.pathID;
        }

        public T FindScript<T>(Predicate<T> condition) where T : BehaviorData
        {
            return FindScript(ao => true, condition);
        }

        public T FindScript<T>(Predicate<MonoBehaviorAssetData> cond, Predicate<T> condition) where T : BehaviorData
        {
            AssetObject obj = GetAssetObjectFromScript(cond, condition);
            return obj != null ? ((obj.data as MonoBehaviorAssetData).data as T) : null;
        }

        public GameObjectAssetData FindGameObject(Predicate<GameObjectAssetData> pred)
        {
            foreach(AssetObject obj in objects) {
                if(!(obj.data is GameObjectAssetData)) continue;
                GameObjectAssetData gobj = (GameObjectAssetData)obj.data;
                if(pred(gobj)) return gobj;
            }
            return null;
        }

        public GameObjectAssetData FindGameObject(string name)
        {
            return FindGameObject(g => g.name == name);
        }

        public LevelCollectionBehaviorData FindExtrasLevelCollection() {
            AssetObject obj = objects[235]; // the index of the extras collection in sharedassets17
            if(!(obj.data is MonoBehaviorAssetData))
                throw new ParseException("Extras level collection not at normal spot");
            MonoBehaviorAssetData monob = (MonoBehaviorAssetData)obj.data;
            if(monob.name != "ExtrasLevelCollection")
                throw new ParseException("Extras level collection not at normal spot");
            return (LevelCollectionBehaviorData)monob.data;
        }

        public LevelPackBehaviorData FindExtrasLevelPack()
        {
            AssetObject obj = objects[236]; // the index of the extras collection in sharedassets17
            if (!(obj.data is MonoBehaviorAssetData))
                throw new ParseException("Extras level pack not at normal spot (not monob)");
            MonoBehaviorAssetData monob = (MonoBehaviorAssetData)obj.data;
            if (monob.name != "ExtrasLevelPack")
                throw new ParseException("Extras level pack not at normal spot");
            return (LevelPackBehaviorData)monob.data;
        }

        public BeatmapLevelPackCollection FindMainLevelPackCollection()
        {
            // This file needs to be sharedassets19.assets for the MainLevelPackCollection
            return FindScript<BeatmapLevelPackCollection>(a => true); // Should only be one.
        }

        private void TryToFindEnvironment(string name) {
            string monobName = name + "SceneInfo";
            AssetObject obj = objects.Find(x => (x.data is MonoBehaviorAssetData) &&
                    ((x.data as MonoBehaviorAssetData).name == monobName));
            if(obj == null) return;
            environmentIDToPtr.Add(name, new AssetPtr(0, obj.pathID));
        }

        private void FindEnvironmentPointers() {
            TryToFindEnvironment("NiceEnvironment");
            TryToFindEnvironment("TriangleEnvironment");
            TryToFindEnvironment("BigMirrorEnvironment");
            TryToFindEnvironment("KDAEnvironment");
            TryToFindEnvironment("CrabRaveEnvironment");

            environmentIDToPtr.Add("DefaultEnvironment", new AssetPtr(20, 1));
            if(!environmentIDToPtr.ContainsKey("NiceEnvironment")) { // v1.0.0
                environmentIDToPtr.Add("NiceEnvironment", new AssetPtr(38, 3));
            }
        }

        public sealed class Transaction {
            private SerializedAssets _assets;

            public Dictionary<byte[],AssetPtr> scriptIDToScriptPtr {
                get { return _assets.scriptIDToScriptPtr; }
            }
            public Dictionary<string,AssetPtr> environmentIDToPtr {
                get { return _assets.environmentIDToPtr; }
            }

            ulong lastPathID;
            List<AssetData> toAdd;

            public Transaction(SerializedAssets assets) {
                lastPathID = (ulong)assets.objects.Count;
                toAdd = new List<AssetData>();
                _assets = assets;
            }

            public AssetPtr AppendAsset(AssetData data) {
                toAdd.Add(data);
                lastPathID += 1;
                return new AssetPtr(0, lastPathID);
            }

            public void ApplyTo(SerializedAssets assets) {
                Debug.Assert((ulong)(assets.objects.Count + toAdd.Count) == lastPathID, "Can't add anything while transaction is live");
                foreach(AssetData obj in toAdd) {
                    assets.AppendAsset(obj);
                }
            }

            public AssetObject GetAssetAt(ulong pathID) {
                return _assets.GetAssetAt(pathID);
            }
        }
    }
}

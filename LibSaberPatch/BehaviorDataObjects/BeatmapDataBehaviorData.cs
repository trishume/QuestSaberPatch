using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class BeatmapDataBehaviorData : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("95AF3C8D406FF35C9DA151E0EE1E0013");
        public static byte[] TypeHash = Utils.HexToBytes("56D73D1BF0CB3F5DA4714C77A41B9F88");

        public string jsonData;
        public byte[] signature;
        public byte[] projectedData;

        public BeatmapDataBehaviorData() { }

        public BeatmapDataBehaviorData(BinaryReader reader, int length)
        {
            jsonData = reader.ReadAlignedString();
            signature = reader.ReadPrefixedBytes();
            projectedData = reader.ReadPrefixedBytes();
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WriteAlignedString(jsonData);
            w.WritePrefixedBytes(signature);
            w.WritePrefixedBytes(projectedData);
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x0E;
        }

        public static BeatmapDataBehaviorData FromJsonFile(string path) {
            string jsonData = File.ReadAllText(path);
            BeatmapSaveData saveData = JsonConvert.DeserializeObject<BeatmapSaveData>(jsonData);
            byte[] projectedData = saveData.SerializeToBinary();

            return new BeatmapDataBehaviorData() {
                jsonData = "",
                signature = new byte[128], // all zeros
                projectedData = projectedData,
            };
        }
    }
}

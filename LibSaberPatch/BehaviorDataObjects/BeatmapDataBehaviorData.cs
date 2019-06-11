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

        public BeatmapDataBehaviorData(BinaryReader reader, int length, Apk.Version v)
        {
            jsonData = reader.ReadAlignedString();
            if(v < Apk.Version.V1_1_0) {
                signature = reader.ReadPrefixedBytes();
                projectedData = reader.ReadPrefixedBytes();
            }
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WriteAlignedString(jsonData);
            if(signature != null && projectedData != null) {
                w.WritePrefixedBytes(signature);
                w.WritePrefixedBytes(projectedData);
            }

            // The base game omits these, which causes a bunch of Unity
            // warnings, but we try to round-trip assets files so don't force them
            // else {
            //     w.WritePrefixedBytes(new byte[0]);
            //     w.WritePrefixedBytes(new byte[0]);
            // }
        }

        public static BeatmapDataBehaviorData FromJsonFile(string path, Apk.Version v) {
            string jsonData = File.ReadAllText(path);
            if(v < Apk.Version.V1_1_0) {
                BeatmapSaveData saveData = JsonConvert.DeserializeObject<BeatmapSaveData>(jsonData);
                byte[] projectedData = saveData.SerializeToBinary();

                return new BeatmapDataBehaviorData() {
                    jsonData = "",
                    signature = new byte[128], // all zeros
                    projectedData = projectedData,
                };
            } else {
                return new BeatmapDataBehaviorData() {
                    jsonData = jsonData,
                };
            }
        }
    }
}

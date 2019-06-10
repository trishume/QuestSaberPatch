using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class BeatmapDataV2BehaviorData : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("95AF3C8D406FF35C9DA151E0EE1E0013");
        public static byte[] TypeHash = Utils.HexToBytes("87650EB74BF6109EE482D14C881EDC21");

        public string jsonData;

        public BeatmapDataV2BehaviorData() { }

        public BeatmapDataV2BehaviorData(BinaryReader reader, int length)
        {
            jsonData = reader.ReadAlignedString();
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WriteAlignedString(jsonData);
        }

        public override int SharedAssetsTypeIndex()
        {
            return 15;
        }

        public static BeatmapDataV2BehaviorData FromJsonFile(string path) {
            string jsonData = File.ReadAllText(path);
            BeatmapSaveData saveData = JsonConvert.DeserializeObject<BeatmapSaveData>(jsonData);

            return new BeatmapDataV2BehaviorData() {
                jsonData = jsonData,
            };
        }
    }
}

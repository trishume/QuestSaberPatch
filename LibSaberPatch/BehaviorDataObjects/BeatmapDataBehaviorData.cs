using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class BeatmapDataBehaviorData : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("95AF3C8D406FF35C9DA151E0EE1E0013");

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

        public override void WriteTo(BinaryWriter w)
        {
            w.WriteAlignedString(jsonData);
            w.WritePrefixedBytes(signature);
            w.WritePrefixedBytes(projectedData);
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x0E;
        }
    }
}

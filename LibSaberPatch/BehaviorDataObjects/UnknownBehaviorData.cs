using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class UnknownBehaviorData : BehaviorData
    {
        public byte[] bytes;
        public UnknownBehaviorData(BinaryReader reader, int length)
        {
            bytes = reader.ReadBytes(length);
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(bytes);
        }

        public override int SharedAssetsTypeIndex()
        {
            throw new ApplicationException("unknown type index");
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
                return bytes.Equals((data as UnknownBehaviorData).bytes);
            return false;
        }
    }
}

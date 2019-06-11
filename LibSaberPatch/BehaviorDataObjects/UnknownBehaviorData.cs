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

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.Write(bytes);
        }
    }
}

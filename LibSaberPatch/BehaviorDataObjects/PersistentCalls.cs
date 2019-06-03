using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class PersistentCalls : BehaviorData
    {
        public List<byte[]> calls;
        public string typeName;

        public PersistentCalls(BinaryReader reader, int _length)
        {
            // Number of bytes in calls:
            int b = 0;
            calls = reader.ReadPrefixedList(r => r.ReadBytes(b));
            typeName = reader.ReadAlignedString();
        }
        public override bool Equals(BehaviorData data)
        {
            //TODO implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            // No type for local data
            return -1;
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.WritePrefixedList(calls, b => w.Write(b));
            w.WriteAlignedString(typeName);
        }
    }
}

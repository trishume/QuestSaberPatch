using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class PersistentCalls
    {
        public List<byte[]> calls;
        public string typeName;

        public PersistentCalls(BinaryReader reader)
        {
            // Number of bytes in calls:
            int b = 0;
            calls = reader.ReadPrefixedList(r => r.ReadBytes(b));
            typeName = reader.ReadAlignedString();
        }

        public void WriteTo(BinaryWriter w)
        {
            w.WritePrefixedList(calls, b => w.Write(b));
            w.WriteAlignedString(typeName);
        }
    }
}

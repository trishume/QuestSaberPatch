using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class AlwaysOwnedBehaviorData : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("4157F52973655C8927CD441D77E8D6CD");

        public List<AssetPtr> levelPacks;
        public List<AssetPtr> levels;

        public AlwaysOwnedBehaviorData(BinaryReader reader, int length)
        {
            levelPacks = reader.ReadPrefixedList(r => new AssetPtr(r));
            levels = reader.ReadPrefixedList(r => new AssetPtr(r));
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WritePrefixedList(levelPacks, x => x.WriteTo(w));
            w.WritePrefixedList(levels, x => x.WriteTo(w));
        }

        public override void Trace(Action<AssetPtr> action)
        {
            foreach (AssetPtr p in levelPacks) {
                action(p);
            }
            foreach (AssetPtr p in levels) {
                action(p);
            }
        }
    }
}

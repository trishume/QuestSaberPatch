using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class LevelCollectionBehaviorData : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("930CDD59C2DBC4FA674501914A570AC2");

        public List<AssetPtr> levels;

        public LevelCollectionBehaviorData()
        {
            levels = new List<AssetPtr>();
        }

        public LevelCollectionBehaviorData(BinaryReader reader, int length)
        {
            levels = reader.ReadPrefixedList(r => new AssetPtr(r));
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WritePrefixedList(levels, x => x.WriteTo(w));
        }

        public override void Trace(Action<AssetPtr> action)
        {
            foreach (AssetPtr p in levels)
            {
                action(p);
            }
        }
    }
}

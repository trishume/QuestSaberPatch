using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class LevelCollectionBehaviorData : BehaviorData
    {
        public const int PathID = 760;

        public List<AssetPtr> levels;

        public LevelCollectionBehaviorData()
        {
            levels = new List<AssetPtr>();
        }

        public LevelCollectionBehaviorData(BinaryReader reader, int length)
        {
            levels = reader.ReadPrefixedList(r => new AssetPtr(r));
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.WritePrefixedList(levels, x => x.WriteTo(w));
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x10;
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
                return levels.Equals((data as LevelCollectionBehaviorData).levels);
            return false;
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

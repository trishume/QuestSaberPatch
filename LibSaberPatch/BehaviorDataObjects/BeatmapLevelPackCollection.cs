using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class BeatmapLevelPackCollection : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("C6A198833B7D41CCE8D783CD6A11BFD4");

        public List<AssetPtr> beatmapLevelPacks;
        public List<AssetPtr> previewBeatmapLevelPack;

        public BeatmapLevelPackCollection(BinaryReader reader, int _length)
        {
            beatmapLevelPacks = reader.ReadPrefixedList(r => new AssetPtr(r));
            previewBeatmapLevelPack = reader.ReadPrefixedList(r => new AssetPtr(r));
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WritePrefixedList(beatmapLevelPacks, a => a.WriteTo(w));
            w.WritePrefixedList(previewBeatmapLevelPack, a => a.WriteTo(w));
        }

        public override void Trace(Action<AssetPtr> action)
        {
            foreach (AssetPtr p in beatmapLevelPacks)
            {
                action(p);
            }
            foreach (AssetPtr p in previewBeatmapLevelPack)
            {
                action(p);
            }
        }
    }
}

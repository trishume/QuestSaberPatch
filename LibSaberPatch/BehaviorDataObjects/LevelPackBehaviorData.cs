using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class LevelPackBehaviorData : BehaviorData
    {
        public const int PathID = 1480;

        public string packID;
        public string packName;
        public AssetPtr coverImage;
        public bool isPackAlwaysOwned;
        public AssetPtr beatmapLevelCollection;


        public LevelPackBehaviorData()
        {
            isPackAlwaysOwned = true;
        }
        public LevelPackBehaviorData(BinaryReader reader, int length)
        {
            packID = reader.ReadAlignedString();
            packName = reader.ReadAlignedString();
            coverImage = new AssetPtr(reader);
            isPackAlwaysOwned = Convert.ToBoolean(reader.ReadByte());
            reader.AlignStream();
            beatmapLevelCollection = new AssetPtr(reader);
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.WriteAlignedString(packID);
            w.WriteAlignedString(packName);
            coverImage.WriteTo(w);
            w.Write(isPackAlwaysOwned);
            w.AlignStream();
            beatmapLevelCollection.WriteTo(w);
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x1C;
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
                return packID == (data as LevelPackBehaviorData).packID;
            return false;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(coverImage);
            action(beatmapLevelCollection);
        }
    }
}

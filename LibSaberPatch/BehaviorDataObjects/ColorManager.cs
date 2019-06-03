using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class ColorManager : BehaviorData
    {
        public const int PathID = 297;

        public AssetPtr playerModel;
        public AssetPtr colorA;
        public AssetPtr colorB;

        public ColorManager(BinaryReader reader, int _length)
        {
            playerModel = new AssetPtr(reader);
            colorA = new AssetPtr(reader);
            colorB = new AssetPtr(reader);
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
            {
                var cm = data as ColorManager;
                return playerModel.Equals(cm.playerModel) && colorA.Equals(cm.colorA) && colorB.Equals(cm.colorB);
            }
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x0E;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(playerModel);
            action(colorA);
            action(colorB);
        }

        public override void WriteTo(BinaryWriter w)
        {
            playerModel.WriteTo(w);
            colorA.WriteTo(w);
            colorB.WriteTo(w);
        }
    }
}

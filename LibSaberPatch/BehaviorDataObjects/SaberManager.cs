using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class SaberManager : BehaviorData
    {
        public const int PathID = 1210;

        public AssetPtr leftSaber;
        public AssetPtr rightSaber;

        public SaberManager(BinaryReader reader, int _length)
        {
            leftSaber = new AssetPtr(reader);
            rightSaber = new AssetPtr(reader);
        }
        public override bool Equals(BehaviorData data)
        {
            //TODO Implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0xE3;
        }

        public override void WriteTo(BinaryWriter w)
        {
            leftSaber.WriteTo(w);
            rightSaber.WriteTo(w);
        }
    }
}

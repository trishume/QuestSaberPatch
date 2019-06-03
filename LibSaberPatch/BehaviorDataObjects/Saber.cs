using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class Saber : BehaviorData
    {
        public const int PathID = 549;

        public AssetPtr topPos;
        public AssetPtr bottomPos;
        public AssetPtr handlePos;
        public AssetPtr vrController;
        public AssetPtr saberTypeObject;

        // Here is the list of components of the Saber's GameObject:
        // Transform, VRController, BoxCollider, Rigidbody, Saber (this), SaberTypeObject, SaberModelContainer
        // MeshFilters of importance: no idea

        public Saber(BinaryReader reader, int _length)
        {
            topPos = new AssetPtr(reader);
            bottomPos = new AssetPtr(reader);
            handlePos = new AssetPtr(reader);
            vrController = new AssetPtr(reader);
            saberTypeObject = new AssetPtr(reader);
        }
        public override bool Equals(BehaviorData data)
        {
            //TODO Implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x07;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(topPos);
            action(handlePos);
            action(bottomPos);
            action(vrController);
            action(saberTypeObject);
        }

        public override void WriteTo(BinaryWriter w)
        {
            topPos.WriteTo(w);
            bottomPos.WriteTo(w);
            handlePos.WriteTo(w);
            vrController.WriteTo(w);
            saberTypeObject.WriteTo(w);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class Saber : BehaviorData
    {
        // LeftSaber: (142, 20) GO, Transform: (142, 54)
        // DEPRECATED
        //public const int PathID = 549;

        public AssetPtr topPos;
        public AssetPtr bottomPos;
        public AssetPtr handlePos;
        public AssetPtr vrController;
        public AssetPtr saberTypeObject;

        // Here is the list of components of the Saber's GameObject:
        // Transform, VRController, BoxCollider, Rigidbody, Saber (this), SaberTypeObject, SaberModelContainer
        // MeshFilters of importance: no idea
        // Children: 2
        // Transforms: (142, 77), (142, 76)
        // GameObjects: (142, 34): Top, (142, 38): Bottom
        // To insert custom saber, should just need to add new gameobject, set that object's parent to saber, disable all old mesh filters in top/bottom

        public Saber(BinaryReader reader, int _length)
        {
            topPos = new AssetPtr(reader);
            bottomPos = new AssetPtr(reader);
            handlePos = new AssetPtr(reader);
            vrController = new AssetPtr(reader);
            saberTypeObject = new AssetPtr(reader);
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(topPos);
            action(handlePos);
            action(bottomPos);
            action(vrController);
            action(saberTypeObject);
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            topPos.WriteTo(w);
            bottomPos.WriteTo(w);
            handlePos.WriteTo(w);
            vrController.WriteTo(w);
            saberTypeObject.WriteTo(w);
        }
    }
}

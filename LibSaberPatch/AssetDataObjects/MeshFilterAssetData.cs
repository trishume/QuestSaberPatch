using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.AssetDataObjects
{
    public class MeshFilterAssetData : AssetData
    {
        public const int ClassID = 33;

        public AssetPtr gameObject;
        public AssetPtr mesh;

        // 16, 19, 33, 36

        public MeshFilterAssetData(BinaryReader reader, int _length)
        {
            gameObject = new AssetPtr(reader);
            mesh = new AssetPtr(reader);
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            gameObject.WriteTo(w);
            mesh.WriteTo(w);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.AssetDataObjects
{
    public sealed class TransformAssetData : AssetData
    {
        public const int ClassID = 4;

        public AssetPtr gameObject;
        public AssetVector4 localRotation;
        public AssetVector3 localPosition;
        public AssetVector3 localScale;
        public List<AssetPtr> children;
        public AssetPtr parent;

        public TransformAssetData(BinaryReader reader, int _length)
        {
            gameObject = new AssetPtr(reader);
            localRotation = new AssetVector4(reader, 16);
            localPosition = new AssetVector3(reader, 12);
            localScale = new AssetVector3(reader, 12);
            children = reader.ReadPrefixedList(r => new AssetPtr(r));
            parent = new AssetPtr(reader);
        }

        public sealed override int SharedAssetsTypeIndex()
        {
            return -1; // TODO
        }

        public sealed override void WriteTo(BinaryWriter w)
        {
            gameObject.WriteTo(w);
            localRotation.WriteTo(w);
            localPosition.WriteTo(w);
            localScale.WriteTo(w);
            w.WritePrefixedList(children, c => c.WriteTo(w));
            parent.WriteTo(w);
        }
    }
}

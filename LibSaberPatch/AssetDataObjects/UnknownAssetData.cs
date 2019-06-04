using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.AssetDataObjects
{
    public class UnknownAssetData : AssetData
    {
        public byte[] bytes;
        public UnknownAssetData(BinaryReader reader, int length)
        {
            bytes = reader.ReadBytes(length);
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(bytes);
        }

        public override int SharedAssetsTypeIndex()
        {
            throw new ApplicationException("unknown type index");
        }

        public override bool Equals(AssetData o)
        {
            if (GetType().Equals(o))
                return bytes.Equals((o as UnknownAssetData).bytes);
            return false;
        }
    }
}

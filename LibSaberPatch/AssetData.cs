using System.IO;

namespace LibSaberPatch
{
    public abstract class AssetData
    {
        public abstract void WriteTo(BinaryWriter w);
    }

    public class UnknownAssetData : AssetData
    {
        public byte[] bytes;
        public UnknownAssetData(byte[] bs) {
            bytes = bs;
        }

        public override void WriteTo(BinaryWriter w) {
            w.Write(bytes);
        }
    }
}

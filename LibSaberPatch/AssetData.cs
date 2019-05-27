namespace LibSaberPatch
{
    public class AssetData
    {
    }

    public class UnknownAssetData : AssetData
    {
        public byte[] bytes;
        public UnknownAssetData(byte[] bs) {
            bytes = bs;
        }
    }
}

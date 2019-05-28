using System;
using System.IO.Compression;
using LibSaberPatch;

namespace app
{
    class Program
    {
        static void Main(string[] args)
        {
            string apkPath = args[0];
            using (ZipArchive archive = ZipFile.Open(apkPath, ZipArchiveMode.Update)) {
                ApkUtils.PatchSignatureCheck(archive);

                string escapeAsset = "assets/bin/Data/3eb70b9e20363dd488f8c4841d7db87f";
                byte[] data = ApkUtils.ReadEntireEntry(archive, escapeAsset);
                SerializedAssets assets = SerializedAssets.FromBytes(data);
                MonoBehaviorAssetData asset = (MonoBehaviorAssetData)assets.objects[0].data;
                LevelBehaviorData level = (LevelBehaviorData)asset.data;
                level.subName = "ft. Summer Hose";
                byte[] newData = assets.ToBytes();
                ApkUtils.WriteEntireEntry(archive, escapeAsset, newData);
            }
        }
    }
}

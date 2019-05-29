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
            using (Apk apk = new Apk(apkPath)) {
                apk.PatchSignatureCheck();

                byte[] data = apk.ReadEntireEntry(Apk.MainAssetsFile);
                SerializedAssets assets = SerializedAssets.FromBytes(data);

                LevelCollectionBehaviorData extrasCollection = assets.FindExtrasLevelCollection();
                for(int i = 1; i < args.Length; i++) {
                    AssetPtr levelPtr = assets.AppendLevelFromFolder(apk, args[i]);
                    extrasCollection.levels.Add(levelPtr);
                }

                byte[] outData = assets.ToBytes();
                apk.ReplaceAssetsFile(Apk.MainAssetsFile, outData);
            }
        }
    }
}

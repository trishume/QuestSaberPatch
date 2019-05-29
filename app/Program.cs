using System;
using System.IO.Compression;
using System.Collections.Generic;
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

                HashSet<string> existingLevels = assets.ExistingLevelIDs();
                LevelCollectionBehaviorData extrasCollection = assets.FindExtrasLevelCollection();
                for(int i = 1; i < args.Length; i++) {
                    JsonLevel level = JsonLevel.LoadFromFolder(args[i]);
                    if(existingLevels.Contains(level.LevelID())) {
                        Console.WriteLine($"Present: {level._songName}");
                        continue;
                    } else {
                        Console.WriteLine($"Adding:  {level._songName}");
                    }
                    AssetPtr levelPtr = level.AddToAssets(assets, apk);
                    extrasCollection.levels.Add(levelPtr);
                }

                byte[] outData = assets.ToBytes();
                apk.ReplaceAssetsFile(Apk.MainAssetsFile, outData);
            }
        }
    }
}

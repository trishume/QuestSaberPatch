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
                    Utils.FindLevels(args[i], levelFolder => {
                        JsonLevel level = JsonLevel.LoadFromFolder(levelFolder);
                        string levelID = level.LevelID();
                        if(existingLevels.Contains(levelID)) {
                            Console.WriteLine($"Present: {level._songName}");
                        } else {
                            Console.WriteLine($"Adding:  {level._songName}");
                            AssetPtr levelPtr = level.AddToAssets(assets, apk);
                            extrasCollection.levels.Add(levelPtr);
                            existingLevels.Add(levelID);
                        }
                    });
                }

                byte[] outData = assets.ToBytes();
                apk.ReplaceAssetsFile(Apk.MainAssetsFile, outData);
            }
        }
    }
}

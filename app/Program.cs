using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using LibSaberPatch;
using Newtonsoft.Json;
using System.Linq;

namespace app
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1) {
                Console.WriteLine("arguments: pathToAPKFileToModify [-r removeSongs] levelFolders...");
                return;
            }
            bool removeSongs = false;
            if (args.Contains("-r") || args.Contains("removeSongs"))
            {
                removeSongs = true;
            }
            string apkPath = args[0];
            using (Apk apk = new Apk(apkPath)) {
                apk.PatchSignatureCheck();

                byte[] data = apk.ReadEntireEntry(Apk.MainAssetsFile);
                SerializedAssets assets = SerializedAssets.FromBytes(data);

                HashSet<string> existingLevels = assets.ExistingLevelIDs();
                LevelCollectionBehaviorData extrasCollection = assets.FindExtrasLevelCollection();
                for(int i = 1; i < args.Length; i++) {
                    if (args[i] == "-r" || args[i] == "removeSongs")
                    {
                        continue;
                    }
                    Utils.FindLevels(args[i], levelFolder => {
                        try {
                            JsonLevel level = JsonLevel.LoadFromFolder(levelFolder);
                            string levelID = level.GenerateBasicLevelID();
                            var apkTxn = new Apk.Transaction();

                            if (existingLevels.Contains(levelID)) {
                                if (removeSongs)
                                {
                                    // Currently does not handle transactions (it half-supports them)
                                    Console.WriteLine($"Removing: {level._songName}");
                                    ulong levelPid = assets.GetLevelMatching(levelID).RemoveFromAssets(assets, apkTxn);
                                    // We also don't _need_ to remove this from the existingLevels, but we probably _should_
                                    existingLevels.Remove(levelID);
                                    
                                    extrasCollection.levels.RemoveAll(ptr => ptr.pathID == levelPid);

                                    apkTxn.ApplyTo(apk);
                                    assets.EndRemoval();
                                } else
                                {
                                    Console.WriteLine($"Present: {level._songName}");
                                }
                            } else {
                                Console.WriteLine($"Adding:  {level._songName}");
                                // We use transactions here so if these throw
                                // an exception, which happens when levels are
                                // invalid, then it doesn't modify the APK in
                                // any way that might screw things up later.
                                var assetsTxn = new SerializedAssets.Transaction(assets);
                                AssetPtr levelPtr = level.AddToAssets(assetsTxn, apkTxn, levelID);

                                // Danger should be over, nothing here should fail
                                assetsTxn.ApplyTo(assets);
                                extrasCollection.levels.Add(levelPtr);
                                existingLevels.Add(levelID);
                                apkTxn.ApplyTo(apk);
                            }
                        } catch (FileNotFoundException e) {
                            Console.WriteLine("[SKIPPING] Missing file referenced by level: {0}", e.FileName);
                        } catch (JsonReaderException e) {
                            Console.WriteLine("[SKIPPING] Invalid level JSON: {0}", e.Message);
                        }
                    });
                }
                byte[] outData = assets.ToBytes();
                apk.ReplaceAssetsFile(Apk.MainAssetsFile, outData);
                Console.WriteLine("Complete!");
            }
        }
    }
}

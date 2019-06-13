using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using LibSaberPatch;
using Newtonsoft.Json;
using LibSaberPatch.BehaviorDataObjects;
using LibSaberPatch.AssetDataObjects;

namespace app
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1) {
                Console.WriteLine("arguments: pathToAPKFileToModify levelFolders...");
                return;
            }
            string apkPath = args[0];
            using (Apk apk = new Apk(apkPath)) {
                apk.PatchSignatureCheck();

                byte[] data = apk.ReadEntireEntry(apk.MainAssetsFile());
                SerializedAssets assets = SerializedAssets.FromBytes(data, apk.version);

                HashSet<string> existingLevels = assets.ExistingLevelIDs();
                LevelCollectionBehaviorData extrasCollection = assets.FindExtrasLevelCollection();
                for(int i = 1; i < args.Length; i++) {
                    Utils.FindLevels(args[i], levelFolder => {
                        try {
                            JsonLevel level = JsonLevel.LoadFromFolder(levelFolder);
                            string levelID = level.GenerateBasicLevelID();
                            if(existingLevels.Contains(levelID)) {
                                Console.WriteLine($"Present: {level._songName}");
                            } else {
                                Console.WriteLine($"Adding:  {level._songName}");
                                // We use transactions here so if these throw
                                // an exception, which happens when levels are
                                // invalid, then it doesn't modify the APK in
                                // any way that might screw things up later.
                                var assetsTxn = new SerializedAssets.Transaction(assets);
                                var apkTxn = new Apk.Transaction();
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
                apk.ReplaceAssetsFile(apk.MainAssetsFile(), outData);

                apk.Save();
            }

            Console.WriteLine("Signing APK...");
            Signer.Sign(apkPath);
        }
    }
}

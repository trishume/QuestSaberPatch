using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
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

                byte[] data = apk.ReadEntireEntry(Apk.MainAssetsFile);
                SerializedAssets assets = SerializedAssets.FromBytes(data);

                HashSet<string> existingLevels = assets.ExistingLevelIDs();
                LevelCollectionBehaviorData extrasCollection = assets.FindExtrasLevelCollection();
                for(int i = 1; i < args.Length; i++) {
                    var serializationFuncs = new List<Func<(JsonLevel, List<List<MonoBehaviorAssetData>>, string, string)>>();
                    Utils.FindLevels(args[i], levelFolder => {
                        try {
                            JsonLevel level = JsonLevel.LoadFromFolder(levelFolder);
                            string levelID = level.GenerateBasicLevelID();
                            if(existingLevels.Contains(levelID)) {
                                Console.WriteLine($"Present: {level._songName}");
                            } else {
                                serializationFuncs.Add(() => {
                                    var levelData = level.ToAssetData(assets.scriptIDToScriptPtr, levelID);
                                    existingLevels.Add(levelID);

                                    Console.WriteLine($"Serialized:  {level._songName}");

                                    return (level, levelData, levelID, level._songName);
                                });
                            }
                        } catch (FileNotFoundException e) {
                            Console.WriteLine("[SKIPPING] Missing file referenced by level: {0}", e.FileName);
                        } catch (JsonReaderException e) {
                            Console.WriteLine("[SKIPPING] Invalid level JSON: {0}", e.Message);
                        }
                    });

                    var serializedAssets = new (JsonLevel, List<List<MonoBehaviorAssetData>>, string, string)[serializationFuncs.Count];

                    Parallel.ForEach(serializationFuncs, (func, state, idx) => {
                        serializedAssets[idx] = func();
                    });

                    var assetsBatchTxn = new SerializedAssets.Transaction(assets);
                    var apkBatchTxn = new Apk.Transaction();

                    foreach (var (level, levelData, levelID, songName) in serializedAssets) {
                        Console.WriteLine($"Adding:  {songName}");

                        var levelPtr = level.AddToAssets(assetsBatchTxn, apkBatchTxn, levelID, levelData);

                        extrasCollection.levels.Add(levelPtr);
                    }

                    assetsBatchTxn.ApplyTo(assets);
                    apkBatchTxn.ApplyTo(apk);
                }

                byte[] outData = assets.ToBytes();
                apk.ReplaceAssetsFile(Apk.MainAssetsFile, outData);
            }

            Console.WriteLine("Signing APK...");
            Signer.Sign(apkPath);
        }
    }
}

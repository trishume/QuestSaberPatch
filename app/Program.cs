using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using LibSaberPatch;
using Newtonsoft.Json;
using System.Linq;
using LibSaberPatch.BehaviorDataObjects;

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
            bool replaceExtras = false;
            if (args.Contains("-e"))
            {
                replaceExtras = true;
            }
            string apkPath = args[0];
            using (Apk apk = new Apk(apkPath)) {
                apk.PatchSignatureCheck();

                byte[] data = apk.ReadEntireEntry(Apk.MainAssetsFile);
                SerializedAssets assets = SerializedAssets.FromBytes(data);

                string colorPath = "assets/bin/Data/sharedassets1.assets";
                SerializedAssets colorAssets = SerializedAssets.FromBytes(apk.ReadEntireEntry(colorPath));

                //string textAssetsPath = "assets/bin/Data/c4dc0d059266d8d47862f46460cf8f31";
                string textAssetsPath = "assets/bin/Data/231368cb9c1d5dd43988f2a85226e7d7";
                SerializedAssets textAssets = SerializedAssets.FromBytes(apk.ReadEntireEntry(textAssetsPath));

                HashSet<string> existingLevels = assets.ExistingLevelIDs();
                LevelCollectionBehaviorData customCollection = assets.FindCustomLevelCollection();
                LevelPackBehaviorData customPack = assets.FindCustomLevelPack();
                ulong customPackPathID = assets.GetAssetObjectFromScript<LevelPackBehaviorData>(mob => mob.name == "CustomLevelPack", b => true).pathID;

                for (int i = 1; i < args.Length; i++) {
                    if (args[i] == "-r" || args[i] == "removeSongs" || args[i] == "-e")
                    {
                        continue;
                    }
                    if (args[i] == "-t")
                    {
                        if (i + 2 >= args.Length)
                        {
                            // There is not enough data after the text
                            // Reset it.
                            //continue;
                        }
                        var ao = textAssets.GetAssetAt(1);
                        TextAsset ta = ao.data as TextAsset;
                        string key = args[i + 1].ToUpper();

                        var segments = Utils.ReadLocaleText(ta.script, new List<char>() { ',', ',', '\n' });

                        //segments.ToList().ForEach(a => Console.Write(a.Trim() + ","));
                        List<string> value;
                        if (!segments.TryGetValue(key.Trim(), out value))
                        {
                            Console.WriteLine($"[ERROR] Could not find key: {key} in text!");
                        }
                        Console.WriteLine($"Found key at index: {key.Trim()} with value: {value[value.Count - 1]}");
                        segments[key.Trim()][value.Count - 1] = args[i + 2];
                        Console.WriteLine($"New value: {args[i + 2]}");
                        Utils.ApplyWatermark(segments);
                        ta.script = Utils.WriteLocaleText(segments, new List<char>() { ',', ',', '\n' });
                        i += 2;
                        apk.ReplaceAssetsFile(textAssetsPath, textAssets.ToBytes());
                        //Console.WriteLine((a.data as TextAsset).script);
                        continue;
                    }
                    if (args[i] == "-c1" || args[i] == "-c2")
                    {
                        if (i + 1 >= args.Length)
                        {
                            // There is nothing after the color
                            // Reset it.
                            Utils.ResetColors(colorAssets);
                            apk.ReplaceAssetsFile(colorPath, colorAssets.ToBytes());
                            continue;
                        }
                        if (!args[i + 1].StartsWith("("))
                        {
                            // Reset it.
                            Utils.ResetColors(colorAssets);
                            apk.ReplaceAssetsFile(colorPath, colorAssets.ToBytes());
                            continue;
                        }
                        if (i + 4 >= args.Length)
                        {
                            Console.WriteLine($"[ERROR] Cannot parse color, not enough colors! Please copy-paste a series of floats");
                            i += 4;
                            continue;
                        }

                        SimpleColor c = new SimpleColor
                        {
                            r = Convert.ToSingle(args[i + 1].Split(',')[0].Replace('(', '0')),
                            g = Convert.ToSingle(args[i + 2].Split(',')[0].Replace('(', '0')),
                            b = Convert.ToSingle(args[i + 3].Split(',')[0].Replace(')', '0')),
                            a = Convert.ToSingle(args[i + 4].Split(',')[0].Replace(')', '.'))
                        };

                        ColorManager dat = Utils.CreateColor(colorAssets, c);

                        var ptr = colorAssets.AppendAsset(new MonoBehaviorAssetData()
                        {
                            data = c,
                            name = "CustomColor" + args[i][args[i].Length - 1],
                            script = new AssetPtr(1, SimpleColor.PathID),
                        });
                        Console.WriteLine($"Created new CustomColor for colorA at PathID: {ptr.pathID}");
                        if (args[i] == "-c1")
                        {
                            dat.colorA = ptr;
                        } else
                        {
                            dat.colorB = ptr;
                        }

                        apk.ReplaceAssetsFile(colorPath, colorAssets.ToBytes());

                        i += 4;
                        continue;
                    }
                    if (args[i] == "-g")
                    {
                        string path = "assets/bin/Data/level11";
                        SerializedAssets a = SerializedAssets.FromBytes(apk.ReadEntireEntry(path));
                        var gameobject = a.FindGameObject("LeftSaber");
                        var script = gameobject.components[4].FollowToScript<Saber>(a);
                        Console.WriteLine($"GameObject: {gameobject}");
                        foreach (AssetPtr p in gameobject.components)
                        {
                            Console.WriteLine($"Component: {p.pathID} followed: {p.Follow(a)}");
                        }
                        Console.WriteLine($"Left saber script: {script}");
                        // Find all objects that have the GameObject: LeftSaber (pathID = 20, fileID = 0 (142))

                        continue;
                    }
                    if (args[i] == "-s")
                    {
                        string cusomCoverFile = args[i + 1];
                        try
                        {
                            Texture2DAssetData dat = assets.GetAssetAt(14).data as Texture2DAssetData;

                            //assets.SetAssetAt(14, dat);
                            var ptr = assets.AppendAsset(Texture2DAssetData.CoverFromImageFile(args[i + 1], "CustomSongs"));
                            Console.WriteLine($"Added Texture at PathID: {ptr.pathID} with new Texture2D from file: {args[i + 1]}");
                            var sPtr = assets.AppendAsset(Utils.CreateSprite(assets, ptr));
                            Console.WriteLine($"Added Sprite at PathID: {sPtr.pathID}!");

                            customPack.coverImage = sPtr;
                        } catch (FileNotFoundException)
                        {
                            Console.WriteLine($"[ERROR] Custom cover file does not exist: {args[i+1]}");
                        }
                        i++;
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
                                    // Currently does not handle transactions
                                    Console.WriteLine($"Removing: {level._songName}");
                                    existingLevels.Remove(levelID);

                                    var l = assets.GetLevelMatching(levelID);
                                    var ao = assets.GetAssetObjectFromScript<LevelBehaviorData>(p => p.levelID == l.levelID);

                                    ulong lastLegitPathID = 201;

                                    // Currently, this removes all songs the very first time it runs, so it is useless to run this
                                    // every iteration
                                    customCollection.levels.RemoveAll(ptr => ptr.pathID > lastLegitPathID);
                                    foreach (string s in l.OwnedFiles(assets))
                                    {
                                        if (apk != null) apk.RemoveFileAt($"assets/bin/Data/{s}");
                                    }

                                    Utils.RemoveLevel(assets, l);

                                    apkTxn.ApplyTo(apk);
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
                                customCollection.levels.Add(levelPtr);
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

                string mainPackFile = "assets/bin/Data/sharedassets19.assets";
                SerializedAssets mainPackAssets = SerializedAssets.FromBytes(apk.ReadEntireEntry(mainPackFile));

                // Modify image to be CustomLevelPack image?
                //customPack.coverImage = new AssetPtr(assets.externals.FindIndex(e => e.pathName == "sharedassets19.assets"))
                // Adds custom pack to the set of all packs
                int fileI = mainPackAssets.externals.FindIndex(e => e.pathName == "sharedassets17.assets") + 1;
                Console.WriteLine($"Found sharedassets17.assets at FileID: {fileI}");
                var mainLevelPack = mainPackAssets.FindMainLevelPackCollection();
                var pointerPacks = mainLevelPack.beatmapLevelPacks[mainLevelPack.beatmapLevelPacks.Count - 1];
                Console.WriteLine($"Original last pack FileID: {pointerPacks.fileID} PathID: {pointerPacks.pathID}");
                if (!mainLevelPack.beatmapLevelPacks.Any(ptr => ptr.fileID == fileI && ptr.pathID == customPackPathID))
                {
                    Console.WriteLine($"Added CustomLevelPack to {mainPackFile}");
                    if (replaceExtras)
                    {
                        Console.WriteLine("Replacing ExtrasPack!");
                        mainLevelPack.beatmapLevelPacks[2] = new AssetPtr(fileI, customPackPathID);
                    }
                    else
                    {
                        Console.WriteLine("Adding as new Pack!");
                        mainLevelPack.beatmapLevelPacks.Add(new AssetPtr(fileI, customPackPathID));
                    }
                }
                pointerPacks = mainLevelPack.beatmapLevelPacks[mainLevelPack.beatmapLevelPacks.Count - 1];
                Console.WriteLine($"New last pack FileID: {pointerPacks.fileID} PathID: {pointerPacks.pathID}");
                apk.ReplaceAssetsFile(mainPackFile, mainPackAssets.ToBytes());

                Console.WriteLine("Complete!");
            }

            Console.WriteLine("Signing APK...");
            Signer.Sign(apkPath);
        }
    }
}

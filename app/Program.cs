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
        static ColorManager CreateColor(SerializedAssets assets, SimpleColor c)
        {
            Console.WriteLine($"Creating CustomColor with r: {c.r} g: {c.g} b: {c.b} a: {c.a}");

            var dat = assets.FindScript<ColorManager>(cm => true); // Should only have one color manager
            //var dat = ((MonoBehaviorAssetData)assets.GetAssetAt(52).data).data as ColorManager;
            if (dat.colorA.pathID != 54)
            {
                Console.WriteLine($"Removed existing CustomColor at PathID: {dat.colorA.pathID}");
                assets.RemoveAssetAt(dat.colorA.pathID);
            }
            return dat;
        }
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
                    if (args[i] == "-c1" || args[i] == "-c2")
                    {
                        if (i + 4 >= args.Length)
                        {
                            Console.WriteLine($"[ERROR] Cannot parse color, not enough colors! Please copy paste a series of floats");
                            i += 4;
                            continue;
                        }

                        string colorPath = "assets/bin/Data/sharedassets1.assets";
                        SerializedAssets colorAssets = SerializedAssets.FromBytes(apk.ReadEntireEntry(colorPath));

                        SimpleColor c = new SimpleColor
                        {
                            r = Convert.ToSingle(args[i + 1].Split(',')[0].Replace('(', '0')),
                            g = Convert.ToSingle(args[i + 2].Split(',')[0].Replace('(', '0')),
                            b = Convert.ToSingle(args[i + 3].Split(',')[0].Replace(')', '0')),
                            a = Convert.ToSingle(args[i + 4].Split(',')[0].Replace(')', '.'))
                        };

                        ColorManager dat = CreateColor(colorAssets, c);

                        var ptr = colorAssets.AppendAsset(new MonoBehaviorAssetData()
                        {
                            data = c,
                            name = "CustomColor" + args[i][args[i].Length - 1],
                            script = new AssetPtr(1, 423),
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
                        apk.WriteSplits(colorPath);

                        i += 4;
                        continue;
                    }
                    if (args[i] == "-s")
                    {
                        string cusomCoverFile = args[i + 1];
                        try
                        {
                            Texture2DAssetData dat = assets.GetAssetAt(45).data as Texture2DAssetData;
                            byte[] customSongsCover = File.ReadAllBytes(args[i + 1]);
                            dat = new Texture2DAssetData()
                            {
                                name = "CustomSongsCover",
                                forcedFallbackFormat = 4,
                                downscaleFallback = 0,
                                width = 1024,
                                height = 1024,
                                completeImageSize = customSongsCover.Length,
                                textureFormat = 34,
                                mipCount = 11,
                                isReadable = false,
                                streamingMips = false,
                                streamingMipsPriority = 0,
                                imageCount = 1,
                                textureDimension = 2,
                                filterMode = 2,
                                mipBias = -1f,
                                anisotropic = 0,
                                wrapU = 1,
                                wrapV = 1,
                                wrapW = 0,
                                lightmapFormat = 6,
                                colorSpace = 1,
                                imageData = customSongsCover,
                                offset = 0,
                                size = 0,
                                path = ""
                            };

                            assets.SetAssetAt(45, dat);

                            Console.WriteLine($"Replacing Texture at PathID: {45} with new Texture2D from file: {args[i + 1]}");

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

                                    extrasCollection.levels.RemoveAll(ptr => ptr.pathID == ao.pathID);
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
                apk.WriteSplits("assets/bin/Data/sharedassets17.assets");
                Console.WriteLine("Complete!");
            }

            Console.WriteLine("Signing APK...");
            Signer.Sign(apkPath);
        }
    }
}

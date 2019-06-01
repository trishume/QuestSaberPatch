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
                LevelPackBehaviorData levelPack = assets.FindExtrasLevelPack();

                int offset = 1;

                if (args[1] == ("-s"))
                {
                    offset = 3;
                    // String copied from @emulamer's tool
                    string s = "CwAAAEV4dHJhc0NvdmVyAAAAAAAAAAAAAACARAAAgEQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIBEAAAAPwAAAD8BAAAAAAAAAKrborQQ7eZHhB371sswB+MgA0UBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADgAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAYAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADAAAAAAAAQACAAIAAQADAAQAAAAOAAAAAAAAAwAAAAAAAAAAAAAAAAEAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABQAAAAAAAAvwAAAD8AAAAAAAAAPwAAAD8AAAAAAAAAvwAAAL8AAAAAAAAAPwAAAL8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIBEAACARAAAAAAAAAAAAACAvwAAgL8AAAAAAACARAAAAEQAAIBEAAAARAAAgD8AAAAAAAAAAA==";
                    byte[] buffer = Convert.FromBase64String(s);
                    byte[] customSongsCover = File.ReadAllBytes(args[2]);
                    Texture2DAssetData assetsTexture2D = new Texture2DAssetData()
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
                    if (assets.FindExtrasLevelPack().coverImage.pathID != 45)
                    {
                        // Not default image, delete old custom
                        //assets.objects.RemoveAt(assets.FindExtrasLevelPack().coverImage.pathID);
                    }
                    var ptr = assets.AppendAsset(assetsTexture2D);
                    levelPack.coverImage = ptr;
                    Console.WriteLine($"Created cover texture from file: {args[2]}");
                }

                for(int i = offset; i < args.Length; i++) {
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
                apk.ReplaceAssetsFile(Apk.MainAssetsFile, outData);
            }
        }
    }
}

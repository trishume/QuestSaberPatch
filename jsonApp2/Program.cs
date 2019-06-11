using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.ComponentModel;
using LibSaberPatch;
using Newtonsoft.Json;
using LibSaberPatch.BehaviorDataObjects;
using LibSaberPatch.AssetDataObjects;

namespace jsonApp
{
    // These are assigned by JSON so disable the never assigned warning
    #pragma warning disable 0649
    class LevelPack {
        public string id;
        public string name;
        public string coverImagePath;
        public List<string> levelIDs;
    }

    class CustomColors {
        public SimpleColor colorA;
        public SimpleColor colorB;
    }

    class Invocation {
        public string apkPath;
        public bool patchSignatureCheck;
        public bool sign;

        public Dictionary<string, string> levels;
        public List<LevelPack> packs;

        public CustomColors colors;
        public Dictionary<string, string> replaceText;
    }
    #pragma warning restore 0649

    class InvocationResult {
        public bool didSignatureCheckPatch;
        public bool didSign;

        public List<string> presentLevels;
        public List<string> installedLevels;
        public List<string> removedLevels;
        public List<string> missingFromPacks;
        public Dictionary<string,string> installSkipped;
        public string error;

        public InvocationResult() {
            didSignatureCheckPatch = false;
            didSign = false;
            installSkipped = new Dictionary<string, string>();
            installedLevels = new List<string>();
            missingFromPacks = new List<string>();
        }
    }

    class Program {
        static void Main(string[] args) {
            string jsonString;
            using (StreamReader reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding)) {
                jsonString = reader.ReadToEnd();
            }
            Invocation inv = JsonConvert.DeserializeObject<Invocation>(jsonString);
            InvocationResult res = Program.RunInvocation(inv);
            string jsonOut = JsonConvert.SerializeObject(res, Formatting.None);
            Console.WriteLine(jsonOut);
        }

        static InvocationResult RunInvocation(Invocation inv) {
            InvocationResult res = new InvocationResult();

            try {
                using (Apk apk = new Apk(inv.apkPath)) {
                    if(inv.patchSignatureCheck) {
                        apk.PatchSignatureCheck();
                        res.didSignatureCheckPatch = true;
                    }

                    SerializedAssets mainAssets = SerializedAssets.FromBytes(
                        apk.ReadEntireEntry(apk.MainAssetsFile()), apk.version
                    );

                    SyncLevels(apk, mainAssets, inv, res);

                    apk.ReplaceAssetsFile(apk.MainAssetsFile(), mainAssets.ToBytes());

                    if(inv.colors != null) {
                        UpdateColors(apk, inv.colors);
                    }

                    if(inv.replaceText != null) {
                        UpdateText(apk, inv.replaceText);
                    }
                }

                if(inv.sign) {
                    Signer.Sign(inv.apkPath);
                    res.didSign = true;
                }
            } catch(Exception e) {
                res.error = e.ToString();
            }

            return res;
        }

        static void SyncLevels(
            Apk apk,
            SerializedAssets mainAssets,
            Invocation inv,
            InvocationResult res
        ) {
            if(inv.levels == null || inv.packs == null)
                throw new ApplicationException("Either the 'levels' or 'packs' key is missing. Note the 'levels' key changed names from 'ensureInstalled' in the new version.");

            Dictionary<string, ulong> existingLevels = mainAssets.FindLevels();
            ulong maxBasePathID = mainAssets.MainAssetsMaxBaseGamePath();

            // === Load root level pack
            SerializedAssets rootPackAssets = SerializedAssets.FromBytes(apk.ReadEntireEntry(apk.RootPackFile()), apk.version);
            string mainFileName = apk.MainAssetsFileName();
            int mainFileI = rootPackAssets.externals.FindIndex(e => e.pathName == mainFileName) + 1;
            BeatmapLevelPackCollection rootLevelPack = rootPackAssets.FindMainLevelPackCollection();
            // Might be null if we're on a version older than v1.1.0
            AlwaysOwnedBehaviorData alwaysOwned = mainAssets.FindScript<AlwaysOwnedBehaviorData>(x => true);

            // === Remove existing custom packs
            rootLevelPack.beatmapLevelPacks.RemoveAll(ptr => ptr.fileID == mainFileI && ptr.pathID > maxBasePathID);
            if(alwaysOwned != null) alwaysOwned.levelPacks.RemoveAll(ptr => ptr.fileID == 0 && ptr.pathID > maxBasePathID);
            LevelPackBehaviorData.RemoveCustomPacksFromEnd(mainAssets);

            // === Remove old-school custom levels from Extras pack
            var extrasCollection = mainAssets.FindExtrasLevelCollection();
            extrasCollection.levels.RemoveAll(ptr => ptr.pathID > maxBasePathID);

            // === Remove existing levels
            var toRemove = new HashSet<string>();
            foreach(var entry in existingLevels) {
                if(inv.levels.ContainsKey(entry.Key)) continue; // requested
                if(entry.Value <= maxBasePathID) continue; // base game level
                toRemove.Add(entry.Key);
            }
            foreach(string levelID in toRemove) {
                var ao = mainAssets.GetAssetObjectFromScript<LevelBehaviorData>(p => p.levelID == levelID);
                var apkTxn = new Apk.Transaction();
                Utils.RemoveLevel(mainAssets, ao, apkTxn);
                apkTxn.ApplyTo(apk);
            }
            res.removedLevels = toRemove.ToList();

            // === Install new levels
            var toInstall = new HashSet<string>();
            foreach(var entry in inv.levels) {
                if(existingLevels.ContainsKey(entry.Key)) continue; // already installed
                toInstall.Add(entry.Key);
            }
            Program.Install(apk, mainAssets, toInstall, res, inv.levels);

            // === Create new custom packs
            Dictionary<string, ulong> availableLevels = mainAssets.FindLevels();
            foreach(LevelPack pack in inv.packs) {
                if(pack.name == null || pack.id == null || pack.levelIDs == null)
                    throw new ApplicationException("Packs require name, id and levelIDs list");
                var txn = new SerializedAssets.Transaction(mainAssets);
                CustomPackInfo info = LevelPackBehaviorData.CreateCustomPack(
                    txn, pack.id, pack.name, pack.coverImagePath
                );
                txn.ApplyTo(mainAssets);

                var customCollection = info.collection.FollowToScript<LevelCollectionBehaviorData>(mainAssets);
                foreach(string levelID in pack.levelIDs) {
                    ulong levelPathID;
                    if(!availableLevels.TryGetValue(levelID, out levelPathID)) {
                        res.missingFromPacks.Add(levelID);
                        continue;
                    }
                    customCollection.levels.Add(new AssetPtr(0, levelPathID));
                }

                rootLevelPack.beatmapLevelPacks.Add(new AssetPtr(mainFileI, info.pack.pathID));
                if(alwaysOwned != null) alwaysOwned.levelPacks.Add(info.pack);
            }
            res.presentLevels = availableLevels.Keys.ToList();

            apk.ReplaceAssetsFile(apk.RootPackFile(), rootPackAssets.ToBytes());
        }

        static void Install(
            Apk apk,
            SerializedAssets assets,
            HashSet<string> toInstall,
            InvocationResult res,
            Dictionary<string, string> levels
        ) {
            foreach(string levelID in toInstall) {
                string levelFolder = levels[levelID];
                try {
                    JsonLevel level = JsonLevel.LoadFromFolder(levelFolder);
                    // We use transactions here so if these throw
                    // an exception, which happens when levels are
                    // invalid, then it doesn't modify the APK in
                    // any way that might screw things up later.
                    var assetsTxn = new SerializedAssets.Transaction(assets);
                    var apkTxn = new Apk.Transaction();
                    AssetPtr levelPtr = level.AddToAssets(assetsTxn, apkTxn, levelID);

                    // Danger should be over, nothing here should fail
                    assetsTxn.ApplyTo(assets);
                    apkTxn.ApplyTo(apk);
                    res.installedLevels.Add(levelID);
                } catch (FileNotFoundException e) {
                    res.installSkipped.Add(levelID, $"Missing file referenced by level: {e.FileName}");
                } catch (JsonReaderException e) {
                    res.installSkipped.Add(levelID, $"Invalid level JSON: {e.Message}");
                }
            }
        }

        static void UpdateColors(Apk apk, CustomColors colors) {
            SerializedAssets colorAssets = SerializedAssets.FromBytes(
                apk.ReadEntireEntry(apk.ColorsFile()), apk.version);
            // There should only be one color manager
            var colorManager = colorAssets.FindScript<ColorManager>(cm => true);
            colorManager.UpdateColor(colorAssets, colors.colorA, ColorManager.ColorSide.A);
            colorManager.UpdateColor(colorAssets, colors.colorB, ColorManager.ColorSide.B);
            apk.ReplaceAssetsFile(apk.ColorsFile(), colorAssets.ToBytes());
        }

        static void UpdateText(Apk apk, Dictionary<string, string> replaceText) {
            SerializedAssets textAssets = SerializedAssets.FromBytes(apk.ReadEntireEntry(apk.TextFile()), apk.version);
            var aotext = textAssets.GetAssetAt(1);
            TextAssetData ta = aotext.data as TextAssetData;
            var segments = ta.ReadLocaleText();
            TextAssetData.ApplyWatermark(segments);

            foreach(var entry in replaceText) {
                Dictionary<string, string> value;
                if (!segments.TryGetValue(entry.Key, out value)) {
                    continue;
                }
                value["ENGLISH"] = entry.Value;
            }

            ta.WriteLocaleText(segments);
            apk.ReplaceAssetsFile(apk.TextFile(), textAssets.ToBytes());
        }
    }
}

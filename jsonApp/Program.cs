using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.ComponentModel;
using LibSaberPatch;
using Newtonsoft.Json;
using LibSaberPatch.BehaviorDataObjects;

namespace jsonApp
{
    class Invocation {
        // These are assigned by JSON so disable the never assigned warning
        #pragma warning disable 0649

        public string apkPath;
        public bool patchSignatureCheck;
        public Dictionary<string, string> ensureInstalled;
        public bool exitAfterward;

        [DefaultValue(false)]
        public bool sign;

        #pragma warning restore 0649
    }

    class InvocationResult {
        public bool didSignatureCheckPatch;
        public bool didSign;

        public List<string> presentLevels;
        public List<string> installedLevels;
        public Dictionary<string,string> installSkipped;
        public string error;

        public InvocationResult() {
            didSignatureCheckPatch = false;
            didSign = false;
            installSkipped = new Dictionary<string, string>();
            installedLevels = new List<string>();
        }
    }

    class Program {
        static void Main(string[] args) {
            while(true) {
                string jsonString = Console.ReadLine();
                Invocation inv = JsonConvert.DeserializeObject<Invocation>(jsonString);
                InvocationResult res = Program.RunInvocation(inv);
                string jsonOut = JsonConvert.SerializeObject(res, Formatting.None);
                Console.WriteLine(jsonOut);
                if(inv.exitAfterward) break;
            }
        }

        static InvocationResult RunInvocation(Invocation inv) {
            InvocationResult res = new InvocationResult();

            try {
                using (Apk apk = new Apk(inv.apkPath)) {
                    if(inv.patchSignatureCheck) {
                        apk.PatchSignatureCheck();
                        res.didSignatureCheckPatch = true;
                    }

                    byte[] data = apk.ReadEntireEntry(apk.MainAssetsFile());
                    SerializedAssets assets = SerializedAssets.FromBytes(data, apk.version);
                    HashSet<string> existingLevels = assets.ExistingLevelIDs();

                    if(inv.ensureInstalled.Count > 0) {
                        Program.EnsureInstalled(apk, assets, existingLevels, res, inv.ensureInstalled);

                        byte[] outData = assets.ToBytes();
                        apk.ReplaceAssetsFile(apk.MainAssetsFile(), outData);
                    }

                    res.presentLevels = existingLevels.ToList();
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

        static void EnsureInstalled(
            Apk apk,
            SerializedAssets assets,
            HashSet<string> existingLevels,
            InvocationResult res,
            Dictionary<string, string> ensureInstalled
        ) {
            LevelCollectionBehaviorData extrasCollection = assets.FindExtrasLevelCollection();
            foreach(KeyValuePair<string, string> entry in ensureInstalled) {
                string levelID = entry.Key;
                string levelFolder = entry.Value;
                try {
                    JsonLevel level = JsonLevel.LoadFromFolder(levelFolder);
                    if(existingLevels.Contains(levelID)) {
                        res.installSkipped.Add(levelID, "Present");
                    } else {
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
                        apkTxn.ApplyTo(apk);

                        existingLevels.Add(levelID);
                        res.installedLevels.Add(levelID);
                    }
                } catch (FileNotFoundException e) {
                    res.installSkipped.Add(levelID, $"Missing file referenced by level: {e.FileName}");
                } catch (JsonReaderException e) {
                    res.installSkipped.Add(levelID, $"Invalid level JSON: {e.Message}");
                }
            }
        }
    }
}

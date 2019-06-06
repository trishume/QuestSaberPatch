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
    // These are assigned by JSON so disable the never assigned warning
    #pragma warning disable 0649
    class LevelPack {
        public string name;
        public string coverImagePath;
        public List<string> levelIDs;
    }

    class Invocation {
        public string apkPath;
        public bool patchSignatureCheck;
        public bool sign;

        public Dictionary<string, string> levels;
        public List<LevelPack> packs;
    }
    #pragma warning restore 0649

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

                    byte[] mainAssetsData = apk.ReadEntireEntry(Apk.MainAssetsFile);
                    SerializedAssets mainAssets = SerializedAssets.FromBytes(mainAssetsData);

                    Dictionary<string, ulong> existingLevels = mainAssets.FindLevels();
                    ulong maxBasePathID = mainAssets.MainAssetsMaxBaseGamePath();

                    // === Remove existing custom packs
                    // TODO

                    // === Remove old-school custom levels from Extras pack
                    // TODO

                    // === Remove existing levels
                    var toRemove = new HashSet<string>();
                    foreach(var entry in existingLevels) {
                        if(inv.levels.ContainsKey(entry.Key)) continue; // requested
                        if(entry.Value <= maxBasePathID) continue; // base game level
                        toRemove.Add(entry.Key);
                    }
                    // TODO remove all levels in toRemove

                    // === Install new levels
                    var toInstall = new HashSet<string>();
                    foreach(var entry in inv.levels) {
                        if(existingLevels.ContainsKey(entry.Key)) continue; // already installed
                        toInstall.Add(entry.Key);
                    }
                    Program.Install(apk, mainAssets, toInstall, res, inv.levels);

                    // === Create new custom packs
                    // TODO

                    byte[] outData = mainAssets.ToBytes();
                    apk.ReplaceAssetsFile(Apk.MainAssetsFile, outData);

                    Dictionary<string, ulong> finalLevels = mainAssets.FindLevels();
                    res.presentLevels = finalLevels.Keys.ToList();
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
    }
}

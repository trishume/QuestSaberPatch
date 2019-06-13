using LibSaberPatch;
using LibSaberPatch.BehaviorDataObjects;
using Xunit;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json;
using LibSaberPatch.AssetDataObjects;

namespace TestApp
{
    public class SerializedAssetTests
    {
        private const string baseAPKPath = "/Users/tristan/BeatSaber/base_testing.apk";

        private string repoPath(string relativePath) {
            return Path.Combine("../../../../", relativePath);
        }

        private SerializedAssets TestRoundTrips(byte[] data, string name, Apk.Version v) {
            // File.WriteAllBytes($"../../../../testoutput/{name}.before.asset", data);
            SerializedAssets assets = SerializedAssets.FromBytes(data, v);
            Assert.NotEmpty(assets.types);
            Assert.NotEmpty(assets.objects);
            byte[] outData = assets.ToBytes();
            // File.WriteAllBytes($"../../../../testoutput/{name}.after.asset", outData);
            Assert.True(System.Linq.Enumerable.SequenceEqual(data, outData));
            return assets;
        }

        [Fact]
        public void TestBasicRoundTrip() {
            using (Apk apk = new Apk(baseAPKPath)) {
                byte[] data = apk.ReadEntireEntry("assets/bin/Data/3eb70b9e20363dd488f8c4841d7db87f");
                TestRoundTrips(data, "basic", apk.version);
            }
        }

        [Fact]
        public void TestBigFile() {
            using (Apk apk = new Apk(baseAPKPath)) {
                byte[] data = apk.ReadEntireEntry(apk.MainAssetsFile());
                var assets = TestRoundTrips(data, "big", apk.version);

                var existing = assets.ExistingLevelIDs();
                Assert.NotEmpty(existing);
                Assert.False(existing.Contains("BUBBLETEA"), "Run tests on a non-patched APK");

                JsonLevel level = JsonLevel.LoadFromFolder(repoPath("testdata/bubble_tea_song/"));

                var assetsTxn = new SerializedAssets.Transaction(assets);
                var apkTxn = new Apk.Transaction();
                AssetPtr levelPtr = level.AddToAssets(assetsTxn, apkTxn, level.GenerateBasicLevelID());
                assetsTxn.ApplyTo(assets);
                // don't apply apkTxn so our tests don't modify the APK

                LevelCollectionBehaviorData extrasCollection = assets.FindExtrasLevelCollection();
                extrasCollection.levels.Add(levelPtr);
                byte[] outData = assets.ToBytes();
                File.WriteAllBytes($"../../../../testoutput/bubble_tea_mod.asset", outData);
            }
        }

        [Fact]
        public void TestLoadBeatmap() {
            using (Apk apk = new Apk(baseAPKPath)) {
                byte[] data = apk.ReadEntireEntry(apk.MainAssetsFile());
                if(apk.version >= Apk.Version.V1_1_0) return;
                SerializedAssets assets = SerializedAssets.FromBytes(data, apk.version);
                SerializedAssets.AssetObject obj = assets.objects[62];
                MonoBehaviorAssetData monob = (MonoBehaviorAssetData)obj.data;
                BeatmapDataBehaviorData beatmap = (BeatmapDataBehaviorData)monob.data;

                using (Stream fileStream = new FileStream(repoPath("testoutput/beatmap_deflated.bin"), FileMode.Create)) {
                    using (MemoryStream memoryStream = new MemoryStream(beatmap.projectedData)) {
                        using (DeflateStream ds = new DeflateStream(memoryStream, CompressionMode.Decompress)) {
                            ds.CopyTo(fileStream);
                        }
                    }
                }


                BeatmapSaveData saveData = BeatmapSaveData.DeserializeFromBinary(beatmap.projectedData);
                Assert.NotEmpty(saveData._notes);
                byte[] outData = saveData.SerializeToBinary(false);
                File.WriteAllBytes(repoPath("testoutput/beatmap_roundtrip.bin"), outData);

                BeatmapSaveData saveData2 = BeatmapSaveData.DeserializeFromBinary(outData, false);
                Assert.NotEmpty(saveData._notes);
                byte[] outData2 = saveData.SerializeToBinary(false);
                File.WriteAllBytes(repoPath("testoutput/beatmap_roundtrip2.bin"), outData);
            }
        }

        [Fact]
        public void TestPackBeatmap() {
            string beatmapFile = repoPath("testdata/bubble_tea_song/Hard.dat");
            string jsonData = File.ReadAllText(beatmapFile);
            BeatmapSaveData saveData = JsonConvert.DeserializeObject<BeatmapSaveData>(jsonData);
            Assert.NotEmpty(saveData._notes);
            byte[] outData = saveData.SerializeToBinary(false);
            File.WriteAllBytes(repoPath("testoutput/bubbletea_serialized.bin"), outData);
        }

        [Fact]
        public void TestCreateCover() {
            string imageFile = repoPath("testdata/bubble_tea_song/cover.jpg");
            Texture2DAssetData cover = Texture2DAssetData.CoverFromImageFile(imageFile, "BUBBLETEA");
            Assert.Equal(262143, cover.completeImageSize);
        }

        [Fact]
        public void TestTextReplaceRoundTrip() {
            using (Apk apk = new Apk(baseAPKPath)) {
                byte[] data = apk.ReadEntireEntry(apk.TextFile());
                SerializedAssets textAssets = SerializedAssets.FromBytes(data, apk.version);
                var aotext = textAssets.GetAssetAt(1);
                TextAssetData ta = aotext.data as TextAssetData;
                string oldScript = ta.script;
                var segments = ta.ReadLocaleText();
                ta.WriteLocaleText(segments);
                Assert.Equal(oldScript, ta.script);
            }
        }
    }
}

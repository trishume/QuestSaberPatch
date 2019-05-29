using LibSaberPatch;
using Xunit;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json;

namespace TestApp
{
    public class SerializedAssetTests
    {
        private const string baseAPKPath = "/Users/tristan/BeatSaber/base_testing.apk";

        private string repoPath(string relativePath) {
            return Path.Combine("../../../../", relativePath);
        }

        private SerializedAssets TestRoundTrips(byte[] data, string name) {
            // File.WriteAllBytes($"../../../../testoutput/{name}.before.asset", data);
            SerializedAssets assets = SerializedAssets.FromBytes(data);
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
                TestRoundTrips(data, "basic");
            }
        }

        [Fact]
        public void TestBigFile() {
            using (Apk apk = new Apk(baseAPKPath)) {
                byte[] data = apk.JoinedContents(Apk.MainAssetsFile);
                var assets = TestRoundTrips(data, "big");

                // pass null as the apk so it doesn't get modified
                AssetPtr levelPtr = assets.AppendLevelFromFolder(null, repoPath("testdata/bubble_tea_song/"));
                LevelCollectionBehaviorData extrasCollection = assets.FindExtrasLevelCollection();
                extrasCollection.levels.Add(levelPtr);
                byte[] outData = assets.ToBytes();
                File.WriteAllBytes($"../../../../testoutput/bubble_tea_mod.asset", outData);
            }
        }
    }
}

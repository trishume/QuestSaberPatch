using LibSaberPatch;
using Xunit;
using System.IO;
using System.IO.Compression;

namespace TestApp
{
    public class SerializedAssetTests
    {
        private const string baseAPKPath = "/Users/tristan/BeatSaber/base_testing.apk";

        private void TestRoundTrips(byte[] data, string name) {
            SerializedAssets assets;
            File.WriteAllBytes($"../../../../testoutput/{name}.before.asset", data);
            using (Stream stream = new MemoryStream(data)) {
                assets = new SerializedAssets(stream);
                Assert.NotEmpty(assets.types);
                Assert.NotEmpty(assets.objects);
            }
            byte[] outData;
            using (MemoryStream stream = new MemoryStream()) {
                assets.WriteTo(stream);
                stream.Close();
                outData = stream.ToArray();
            }
            File.WriteAllBytes($"../../../../testoutput/{name}.after.asset", outData);
            Assert.True(System.Linq.Enumerable.SequenceEqual(data, outData));
        }

        [Fact]
        public void TestBasicRoundTrip() {
            using (ZipArchive archive = ZipFile.Open(baseAPKPath, ZipArchiveMode.Read)) {
                byte[] data = ApkUtils.ReadEntireEntry(archive, "assets/bin/Data/3eb70b9e20363dd488f8c4841d7db87f");
                TestRoundTrips(data, "basic");
            }
        }

        [Fact]
        public void TestBigRoundTrip() {
            using (ZipArchive archive = ZipFile.Open(baseAPKPath, ZipArchiveMode.Read)) {
                byte[] data = ApkUtils.JoinedContents(archive, "assets/bin/Data/sharedassets17.assets");
                TestRoundTrips(data, "big");
            }
        }
    }
}

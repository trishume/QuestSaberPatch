using LibSaberPatch;
using Xunit;
using System.IO;

namespace TestApp
{
    public class SerializedAssetTests
    {
        private const string baseAPKUnpackedFolderPath = "/Users/tristan/BeatSaber/base_apk";

        private void TestRoundTrips(byte[] data) {
            SerializedAssets assets;
            using (Stream stream = new MemoryStream(data)) {
                assets = new SerializedAssets(stream);
            }
            byte[] outData;
            using (MemoryStream stream = new MemoryStream()) {
                assets.WriteTo(stream);
                stream.Close();
                outData = stream.ToArray();
            }
            Assert.True(System.Linq.Enumerable.SequenceEqual(data, outData));
        }

        [Fact]
        public void TestBasicFile() {
            string path = $"{baseAPKUnpackedFolderPath}/assets/bin/Data/f6e3dc8bf93e55f4a98dc3b594aa0487";
            Stream stream = new FileStream(path, FileMode.Open);
            SerializedAssets assets = new SerializedAssets(stream);
            Assert.Equal(1, assets.types.Count);
            Assert.Equal(1, assets.objects.Count);
        }

        [Fact]
        public void TestBasicRoundTrip() {
            string path = $"{baseAPKUnpackedFolderPath}/assets/bin/Data/f6e3dc8bf93e55f4a98dc3b594aa0487";
            byte[] data = File.ReadAllBytes(path);
            TestRoundTrips(data);
        }

        [Fact]
        public void TestBigRoundTrip() {
            string path = $"{baseAPKUnpackedFolderPath}/assets/bin/Data/sharedassets17.assets";
            byte[] data = SerializedAssets.JoinedContents(path);
            TestRoundTrips(data);
        }

        [Fact]
        public void TestBigFile() {
            string path = $"{baseAPKUnpackedFolderPath}/assets/bin/Data/sharedassets17.assets";
            byte[] data = SerializedAssets.JoinedContents(path);
            using (Stream stream = new MemoryStream(data)) {
                SerializedAssets assets = new SerializedAssets(stream);
                Assert.Equal(30, assets.types.Count);
                Assert.Equal(260, assets.objects.Count);
            }
        }
    }
}

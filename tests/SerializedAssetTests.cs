using LibSaberPatch;
using Xunit;
using System.IO;

namespace TestApp
{
    public class SerializedAssetTests
    {
        private const string baseAPKUnpackedFolderPath = "/Users/tristan/BeatSaber/base_apk";

        [Fact]
        public void TestBasicFile() {
            string path = $"{baseAPKUnpackedFolderPath}/assets/bin/Data/f6e3dc8bf93e55f4a98dc3b594aa0487";
            Stream stream = new FileStream(path, FileMode.Open);
            SerializedAssets assets = new SerializedAssets(stream);
            Assert.Equal(1, assets.types.Count);
            Assert.Equal(1, assets.objects.Count);
        }

        [Fact]
        public void TestBigFile() {
            string path = $"{baseAPKUnpackedFolderPath}/assets/bin/Data/sharedassets17.assets";
            Stream stream = SerializedAssets.JoinedStream(path);
            SerializedAssets assets = new SerializedAssets(stream);
            Assert.Equal(30, assets.types.Count);
            Assert.Equal(260, assets.objects.Count);
        }
    }
}

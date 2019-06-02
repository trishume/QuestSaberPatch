using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace LibSaberPatch
{
    public static class Utils {
        public static void FindLevels(string startDir, Action<string> del) {
            string infoPath = Path.Combine(startDir, "info.dat");
            if(File.Exists(infoPath)) {
                del(startDir);
            } else {
                foreach (string d in Directory.GetDirectories(startDir)) {
                    FindLevels(d, del);
                }
            }
        }

        public static byte[] ImageFileToMipData(string imagePath, int topDim) {
            // pre-compute size of all mips together
            int totalSize = 0;
            for(int dim = topDim; dim > 0; dim /= 2) {
                totalSize += dim*dim;
            }
            totalSize *= 3; // 3 bytes per pixel

            byte[] imageData = new byte[totalSize];
            Span<byte> imageDataSpan = imageData;
            using (Image<Rgb24> image = Image.Load<Rgb24>(Configuration.Default, imagePath)) {
                int dataWriteIndex = 0;
                for(int dim = topDim; dim > 0; dim /= 2) {
                    image.Mutate(x => x.Resize(dim, dim));
                    for (int y = 0; y < image.Height; y++) {
                        // need to do rows in reverse order to match what Unity wants
                        Span<Rgb24> rowPixels = image.GetPixelRowSpan((image.Height-1)-y);
                        Span<byte> rowData = MemoryMarshal.AsBytes(rowPixels);
                        rowData.CopyTo(imageDataSpan.Slice(dataWriteIndex, rowData.Length));
                        dataWriteIndex += rowData.Length;
                    }
                }
            }
            return imageData;
        }
        
        public static ulong RemoveLevel(SerializedAssets assets, LevelBehaviorData level)
        {
            // We want to find the level object in the assets list of objects so that we can remove it via PathID.
            // Well, this is quite a messy solution... But it _should work_...
            // What this is doing: Removing the asset that is a monobehavior, and the monobehavior's data equals this level.
            // Then it casts that to a level behavior data.

            // TODO Make this work with Transactions instead of an assets object.

            // Also remove difficulty beatmaps
            foreach (BeatmapSet s in level.difficultyBeatmapSets)
            {
                foreach (BeatmapDifficulty d in s.difficultyBeatmaps)
                {
                    assets.RemoveAssetAt(d.beatmapData.pathID);
                }
            }
            // Remove cover image
            assets.RemoveAssetAt(level.coverImage.pathID);
            // Remove the file for the audio asset and the audio clip
            var audioAsset = assets.RemoveAssetAt(level.audioClip.pathID).data as AudioClipAssetData;
            if (audioAsset == null)
            {
                throw new ApplicationException($"Could not find audio asset at PathID: {level.audioClip.pathID}");
            }

            // Remove itself!
            ulong levelPathID = assets.RemoveAsset(ao => ao.data.GetType().Equals(typeof(MonoBehaviorAssetData))
            && (ao.data as MonoBehaviorAssetData).name == level.levelID + "Level").pathID;
            return levelPathID;
        }
    }
}

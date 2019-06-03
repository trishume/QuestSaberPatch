using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using LibSaberPatch.BehaviorDataObjects;

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

        public static ColorManager CreateColor(SerializedAssets assets, SimpleColor c)
        {
            Console.WriteLine($"Creating CustomColor with r: {c.r} g: {c.g} b: {c.b} a: {c.a}");

            var dat = assets.FindScript<ColorManager>(cm => true); // Should only have one color manager
            //var dat = ((MonoBehaviorAssetData)assets.GetAssetAt(52).data).data as ColorManager;
            if (dat.colorA.pathID != 54)
            {
                Console.WriteLine($"Removed existing CustomColor at PathID: {dat.colorA.pathID}");
                assets.RemoveAssetAt(dat.colorA.pathID);
            }
            if (dat.colorB.pathID != 53)
            {
                Console.WriteLine($"Removing existing CustomColor at PathID: {dat.colorB.pathID}");
                assets.RemoveAssetAt(dat.colorB.pathID);
            }
            return dat;
        }

        public static void ResetColors(SerializedAssets assets)
        {
            ColorManager manager = assets.FindScript<ColorManager>(cm => true); // Should only have one color manager
            if (manager.colorA.pathID != 54)
            {
                Console.WriteLine($"Removing CustomColor at PathID: {manager.colorA.pathID}");
                assets.RemoveAssetAt(manager.colorA.pathID);
                manager.colorA.pathID = 54;
            }
            if (manager.colorB.pathID != 53)
            {
                Console.WriteLine($"Removing CustomColor at PathID: {manager.colorB.pathID}");
                assets.RemoveAssetAt(manager.colorB.pathID);
                manager.colorB.pathID = 53;
            }
        }

        public static Texture2DAssetData CreateTexture(byte[] customSongsCover)
        {
            return new Texture2DAssetData()
            {
                name = "CustomPackCoverTexture",
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
        }

        public static SpriteAssetData CreateSprite(SerializedAssets assets, AssetPtr customTexture)
        {
            // Default Sprite
            ulong pd = 45;
            var sp = assets.GetAssetAt(pd);
            if (!sp.data.GetType().Equals(typeof(SpriteAssetData)))
            {
                Console.WriteLine($"[ERROR] Default Sprite data does not exist at PathID: {pd} instead it has Type {sp.data.GetType()} with TypeID: {sp.typeID} and classid: {assets.types[sp.typeID].classID}");
            }
            var sprite = sp.data as SpriteAssetData;
            return new SpriteAssetData()
            {
                name = "CustomPackCover",
                texture = customTexture,
                atlasTags = sprite.atlasTags,
                extrude = sprite.extrude,
                floats = sprite.floats,
                guid = sprite.guid,
                isPolygon = sprite.isPolygon,
                second = sprite.second,
                spriteAtlas = sprite.spriteAtlas,
                bytesAfterTexture = sprite.bytesAfterTexture
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.AssetDataObjects
{
    public class Texture2DAssetData : AssetData
    {
        public const int ClassID = 28;

        public string name;
        public int forcedFallbackFormat;
        public int downscaleFallback;
        public int width;
        public int height;
        public int completeImageSize;
        public int textureFormat;
        public int mipCount;
        public bool isReadable;
        public bool streamingMips;

        public int streamingMipsPriority;
        public int imageCount;
        public int textureDimension;

        public int filterMode;
        public int anisotropic;
        public float mipBias;
        public int wrapU;
        public int wrapV;
        public int wrapW;

        public int lightmapFormat;
        public int colorSpace;
        public byte[] imageData;

        public int offset;
        public int size;
        public string path;

        private const int CoverPowerOfTwo = 8;
        private const int PackCoverPowerOfTwo = 10;
        public static Texture2DAssetData CoverFromImageFile(string filePath, string levelID, bool isPackCover = false)
        {
            int powerOfTwo = isPackCover ? PackCoverPowerOfTwo : CoverPowerOfTwo;
            int coverDim = 1 << powerOfTwo;
            byte[] imageData = Utils.ImageFileToMipData(filePath, coverDim);
            return new Texture2DAssetData()
            {
                name = levelID + "Cover",
                forcedFallbackFormat = 4,
                downscaleFallback = 0,
                width = coverDim,
                height = coverDim,
                completeImageSize = imageData.Length,
                textureFormat = 3,
                mipCount = powerOfTwo + 1,
                isReadable = false,
                streamingMips = false,
                streamingMipsPriority = 0,
                imageCount = 1,
                textureDimension = 2,
                filterMode = 2,
                anisotropic = 1,
                mipBias = -1,
                wrapU = 1,
                wrapV = 1,
                wrapW = 0,
                lightmapFormat = 6,
                colorSpace = 1,
                imageData = imageData,
                offset = 0,
                size = 0,
                path = "",
            };
        }

        public Texture2DAssetData() { }

        public Texture2DAssetData(BinaryReader reader, int _length)
        {
            name = reader.ReadAlignedString();
            forcedFallbackFormat = reader.ReadInt32();
            downscaleFallback = reader.ReadInt32();
            width = reader.ReadInt32();
            height = reader.ReadInt32();
            completeImageSize = reader.ReadInt32();
            textureFormat = reader.ReadInt32();
            mipCount = reader.ReadInt32();
            isReadable = reader.ReadBoolean();
            streamingMips = reader.ReadBoolean();
            reader.AlignStream();

            streamingMipsPriority = reader.ReadInt32();
            imageCount = reader.ReadInt32();
            textureDimension = reader.ReadInt32();

            filterMode = reader.ReadInt32();
            anisotropic = reader.ReadInt32();
            mipBias = reader.ReadSingle();
            wrapU = reader.ReadInt32();
            wrapV = reader.ReadInt32();
            wrapW = reader.ReadInt32();

            lightmapFormat = reader.ReadInt32();
            colorSpace = reader.ReadInt32();
            imageData = reader.ReadPrefixedBytes();

            offset = reader.ReadInt32();
            size = reader.ReadInt32();
            path = reader.ReadAlignedString();
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WriteAlignedString(name);
            w.Write(forcedFallbackFormat);
            w.Write(downscaleFallback);
            w.Write(width);
            w.Write(height);
            w.Write(completeImageSize);
            w.Write(textureFormat);
            w.Write(mipCount);
            w.Write(isReadable);
            w.Write(streamingMips);
            w.AlignStream();

            w.Write(streamingMipsPriority);
            w.Write(imageCount);
            w.Write(textureDimension);

            w.Write(filterMode);
            w.Write(anisotropic);
            w.Write(mipBias);
            w.Write(wrapU);
            w.Write(wrapV);
            w.Write(wrapW);

            w.Write(lightmapFormat);
            w.Write(colorSpace);
            w.WritePrefixedBytes(imageData);

            w.Write(offset);
            w.Write(size);
            w.WriteAlignedString(path);
        }
    }
}

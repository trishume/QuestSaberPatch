using System;
using System.IO;

namespace LibSaberPatch
{
    public class AssetPtr
    {
        public int fileID;
        public ulong pathID;

        public AssetPtr(int _fileID, ulong _pathID) {
            fileID = _fileID;
            pathID = _pathID;
        }

        public AssetPtr(BinaryReader reader) {
            fileID = reader.ReadInt32();
            pathID = reader.ReadUInt64();
        }

        public void WriteTo(BinaryWriter w) {
            w.Write(fileID);
            w.Write(pathID);
        }
    }

    public abstract class AssetData
    {
        public abstract void WriteTo(BinaryWriter w);
        public abstract int SharedAssetsTypeIndex();
    }

    public class UnknownAssetData : AssetData
    {
        public byte[] bytes;
        public UnknownAssetData(BinaryReader reader, int length) {
            bytes = reader.ReadBytes(length);
        }

        public override void WriteTo(BinaryWriter w) {
            w.Write(bytes);
        }

        public override int SharedAssetsTypeIndex() {
            throw new ApplicationException("unknown type index");
        }
    }

    public class MonoBehaviorAssetData : AssetData
    {
        public const int ClassID = 114;

        public AssetPtr gameObject; // always zero AFAIK
        public int enabled;
        public AssetPtr script;
        public string name;
        public BehaviorData data;

        public MonoBehaviorAssetData() {
            gameObject = new AssetPtr(0,0);
            enabled = 1;
        }

        public MonoBehaviorAssetData(BinaryReader reader, int length) {
            int startOffset = (int)reader.BaseStream.Position;
            gameObject = new AssetPtr(reader);
            enabled = reader.ReadInt32();
            script = new AssetPtr(reader);
            name = reader.ReadAlignedString();
            int headerLen = (int)reader.BaseStream.Position - startOffset;

            switch(script.pathID) {
                case LevelBehaviorData.PathID:
                    data = new LevelBehaviorData(reader, length - headerLen);
                    break;
                case LevelCollectionBehaviorData.PathID:
                    data = new LevelCollectionBehaviorData(reader, length - headerLen);
                    break;
                case BeatmapDataBehaviorData.PathID:
                    data = new BeatmapDataBehaviorData(reader, length - headerLen);
                    break;
                default:
                    data = new UnknownBehaviorData(reader, length - headerLen);
                    break;
            }
        }

        public override void WriteTo(BinaryWriter w) {
            gameObject.WriteTo(w);
            w.Write(enabled);
            script.WriteTo(w);
            w.WriteAlignedString(name);
            data.WriteTo(w);
        }

        public override int SharedAssetsTypeIndex() {
            return data.SharedAssetsTypeIndex();
        }
    }

    public class AudioClipAssetData : AssetData
    {
        public const int ClassID = 83;

        public string name;
        public int loadType;
        public int channels;
        public int frequency;
        public int bitsPerSample;
        public float length;
        public bool isTracker;

        public int subsoundIndex;
        public bool preloadAudio;
        public bool backgroundLoad;
        public bool legacy3D;

        public string source;
        public ulong offset;
        public ulong size;
        // 0 = PCM, 1 = Vorbis, 3 = MP3, ...
        public int compressionFormat;

        public AudioClipAssetData() {}

        public AudioClipAssetData(BinaryReader reader, int _length) {
            name = reader.ReadAlignedString();
            loadType = reader.ReadInt32();
            channels = reader.ReadInt32();
            frequency = reader.ReadInt32();
            bitsPerSample = reader.ReadInt32();
            length = reader.ReadSingle();
            isTracker = reader.ReadBoolean();

            reader.AlignStream();
            subsoundIndex = reader.ReadInt32();
            preloadAudio = reader.ReadBoolean();
            backgroundLoad = reader.ReadBoolean();
            legacy3D = reader.ReadBoolean();

            reader.AlignStream();
            source = reader.ReadAlignedString();
            offset = reader.ReadUInt64();
            size = reader.ReadUInt64();
            compressionFormat = reader.ReadInt32();
        }

        public override void WriteTo(BinaryWriter w) {
            w.WriteAlignedString(name);
            w.Write(loadType);
            w.Write(channels);
            w.Write(frequency);
            w.Write(bitsPerSample);
            w.Write(length);
            w.Write(isTracker);

            w.AlignStream();
            w.Write(subsoundIndex);
            w.Write(preloadAudio);
            w.Write(backgroundLoad);
            w.Write(legacy3D);

            w.AlignStream();
            w.WriteAlignedString(source);
            w.Write(offset);
            w.Write(size);
            w.Write(compressionFormat);
        }

        public override int SharedAssetsTypeIndex() {
            return 5;
        }
    }

    public class Texture2DAssetData : AssetData
    {
        public const int ClassID = 28;

        string name;
        int forcedFallbackFormat;
        int downscaleFallback;
        int width;
        int height;
        int completeImageSize;
        int textureFormat;
        int mipCount;
        bool isReadable;
        bool streamingMips;

        int streamingMipsPriority;
        int imageCount;
        int textureDimension;

        int filterMode;
        int anisotropic;
        float mipBias;
        int wrapU;
        int wrapV;
        int wrapW;

        int lightmapFormat;
        int colorSpace;
        byte[] imageData;

        int offset;
        int size;
        string path;

        public Texture2DAssetData() {}

        public Texture2DAssetData(BinaryReader reader, int _length) {
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

        public override void WriteTo(BinaryWriter w) {
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

        public override int SharedAssetsTypeIndex() {
            return 2;
        }
    }
}

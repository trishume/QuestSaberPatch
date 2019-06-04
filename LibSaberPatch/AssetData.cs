using LibSaberPatch.BehaviorDataObjects;
using System;
using System.Collections.Generic;
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

        public AssetData Follow(SerializedAssets assets)
        {
            return assets.objects.FindLast(ao => ao.pathID == pathID).data;
        }

        public T Follow<T>(SerializedAssets assets) where T : AssetData
        {
            return (T)Follow(assets);
        }

        public T FollowToScript<T>(SerializedAssets assets) where T : BehaviorData
        {
            return (T)Follow<MonoBehaviorAssetData>(assets).data;
        }

        public void WriteTo(BinaryWriter w) {
            w.Write(fileID);
            w.Write(pathID);
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj.GetType().Equals(this))
            {
                var o = obj as AssetPtr;
                return fileID == o.fileID && pathID == o.pathID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (int)pathID + 65535 * fileID;
        }
    }

    public abstract class AssetData
    {
        public abstract void WriteTo(BinaryWriter w);
        public abstract int SharedAssetsTypeIndex();
        public abstract bool Equals(AssetData o);
        // Could also maybe make this method an actual method, instead of abstract, and use reflection.
        public virtual void Trace(Action<AssetPtr> action)
        {
            // Defaults to nothing.
        }
        /// <summary>
        /// Returns a list of all owned files of this AssetData, it also checks its AssetPtrs.
        /// </summary>
        /// <param name="action">Returns a list of all owned files.</param>
        public virtual List<string> OwnedFiles(SerializedAssets assets)
        {
            // Default to return no owned files
            return new List<string>();
        }
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

        public override bool Equals(AssetData o)
        {
            if (GetType().Equals(o))
                return bytes.Equals((o as UnknownAssetData).bytes);
            return false;
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
                case LevelPackBehaviorData.PathID:
                    data = new LevelPackBehaviorData(reader, length - headerLen);
                    break;
                case ColorManager.PathID:
                    data = new ColorManager(reader, length - headerLen);
                    break;
                case SimpleColor.PathID:
                    data = new SimpleColor(reader, length - headerLen);
                    break;
                case TextMeshPro.PathID:
                    data = new TextMeshPro(reader, length - headerLen);
                    break;
                case Saber.PathID:
                    data = new Saber(reader, length - headerLen);
                    break;
                case BeatmapLevelPackCollection.PathID:
                    data = new BeatmapLevelPackCollection(reader, length - headerLen);
                    break;
                case LocalizedTextMeshProUGUI.PathID:
                    data = new LocalizedTextMeshProUGUI(reader, length - headerLen);
                    break;
                case Localization.PathID:
                    data = new Localization(reader, length - headerLen);
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

        public override bool Equals(AssetData o)
        {
            if (GetType().Equals(o))
                return script.pathID == (o as MonoBehaviorAssetData).script.pathID && name == (o as MonoBehaviorAssetData).name;
            return false;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            // So, we have AssetPtrs, however, we don't want to delete any of the pointers for Gameobject/Script
            // We DO want to delete/call trace on all pointers that are in data.
            data.Trace(action);
        }

        public override List<string> OwnedFiles(SerializedAssets assets)
        {
            return data.OwnedFiles(assets);
        }
    }

    public class MeshFilter : AssetData
    {
        public const int ClassID = 33;

        public AssetPtr gameObject;
        public AssetPtr mesh;

        // 16, 19, 33, 36

        public MeshFilter(BinaryReader reader, int _length)
        {
            gameObject = new AssetPtr(reader);
            mesh = new AssetPtr(reader);
        }
        public override bool Equals(AssetData o)
        {
            //TODO Implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 4;
        }

        public override void WriteTo(BinaryWriter w)
        {
            gameObject.WriteTo(w);
            mesh.WriteTo(w);
        }
    }

    public class GameObjectAssetData : AssetData
    {
        public const int ClassID = 1;

        public AssetPtr[] components;
        public uint layer;
        public string name;
        public ushort tag;
        public bool isActive;

        public GameObjectAssetData(BinaryReader reader, int _length)
        {
            int size = reader.ReadInt32();
            components = new AssetPtr[size];
            for (int i = 0; i < size; i++)
            {
                components[i] = new AssetPtr(reader);
            }
            layer = reader.ReadUInt32();
            name = reader.ReadAlignedString();
            tag = reader.ReadUInt16();
            isActive = reader.ReadBoolean();
        }

        public override bool Equals(AssetData o)
        {
            //TODO Implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0;
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(components.Length);
            foreach (AssetPtr p in components)
            {
                p.WriteTo(w);
            }
            w.Write(layer);
            w.WriteAlignedString(name);
            w.Write(tag);
            w.Write(isActive);
        }
    }

    public class AssetVector3 : AssetData
    {
        public float x;
        public float y;
        public float z;

        public AssetVector3(BinaryReader reader, int _length)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
        }

        public override bool Equals(AssetData o)
        {
            //TODO implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            // No type for local data
            return -1;
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(x);
            w.Write(y);
            w.Write(z);
        }
    }

    public class AssetVector4 : AssetData
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public AssetVector4(BinaryReader reader, int _length)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            w = reader.ReadSingle();
        }

        public override bool Equals(AssetData o)
        {
            //TODO implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            // No type for local data
            return -1;
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(x);
            w.Write(y);
            w.Write(z);
            w.Write(this.w);
        }
    }

    public class TransformAssetData : AssetData
    {
        public const int ClassID = 4;

        public AssetPtr gameObject;
        public AssetVector4 localRotation;
        public AssetVector3 localPosition;
        public AssetVector3 localScale;
        public List<AssetPtr> children;
        public AssetPtr parent;

        public TransformAssetData(BinaryReader reader, int _length)
        {
            gameObject = new AssetPtr(reader);
            localRotation = new AssetVector4(reader, 16);
            localPosition = new AssetVector3(reader, 12);
            localScale = new AssetVector3(reader, 12);
            children = reader.ReadPrefixedList(r => new AssetPtr(r));
            parent = new AssetPtr(reader);
        }

        public override bool Equals(AssetData o)
        {
            //TODO implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return -1;
        }

        public override void WriteTo(BinaryWriter w)
        {
            gameObject.WriteTo(w);
            localRotation.WriteTo(w);
            localPosition.WriteTo(w);
            localScale.WriteTo(w);
            w.WritePrefixedList(children, c => c.WriteTo(w));
            parent.WriteTo(w);
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

        public override bool Equals(AssetData o)
        {
            if (GetType().Equals(o))
                return source == (o as AudioClipAssetData).source && name == (o as AudioClipAssetData).name;
            return false;
        }

        public override List<string> OwnedFiles(SerializedAssets assets)
        {
            return new List<string>() { source };
        }
    }

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

        private const int CoverPowerOfTwo = 10;
        private const int PackCoverPowerOfTwo = 2;
        public static Texture2DAssetData CoverFromImageFile(string filePath, string levelID, bool isPackCover = false) {
            int coverDim = isPackCover ? 1 << PackCoverPowerOfTwo : 1 << CoverPowerOfTwo;
            byte[] imageData = Utils.ImageFileToMipData(filePath, coverDim);
            return new Texture2DAssetData() {
                name = levelID + "Cover",
                forcedFallbackFormat = 4,
                downscaleFallback = 0,
                width = coverDim,
                height = coverDim,
                completeImageSize = imageData.Length,
                textureFormat = 3,
                mipCount = isPackCover ? PackCoverPowerOfTwo + 1 : CoverPowerOfTwo + 1,
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

        public override bool Equals(AssetData o)
        {
            if (GetType().Equals(o))
                return imageData.Equals((o as Texture2DAssetData).imageData);
            return false;
        }
    }

    public class SpriteAssetData : AssetData
    {
        public const int ClassID = 0xD5;

        public string name;
        public float[] floats;
        public uint extrude;
        public bool isPolygon;
        public uint[] guid;
        public long second;
        public List<AssetPtr> atlasTags;
        public AssetPtr spriteAtlas;
        public AssetPtr texture;
        public byte[] bytesAfterTexture;

        public SpriteAssetData(BinaryReader reader, int _length)
        {
            long start = reader.BaseStream.Position;
            name = reader.ReadAlignedString();
            // Rect, Vector2, Vector4, float, Vector2
            floats = new float[13];
            for (int i = 0; i < floats.Length; i++)
            {
                floats[i] = reader.ReadSingle();
            }
            extrude = reader.ReadUInt32();
            isPolygon = reader.ReadBoolean();
            reader.AlignStream();
            guid = new uint[4];
            for (int i = 0; i < guid.Length; i++)
            {
                guid[i] = reader.ReadUInt32();
            }
            second = reader.ReadInt64();
            atlasTags = reader.ReadPrefixedList(r => new AssetPtr(r));
            spriteAtlas = new AssetPtr(reader); // SpriteAtlas
            texture = new AssetPtr(reader);
            bytesAfterTexture = reader.ReadBytes((int)(_length - (reader.BaseStream.Position - start)));
        }

        public SpriteAssetData()
        {
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.WriteAlignedString(name);
            foreach (float f in floats)
            {
                w.Write(f);
            }
            w.Write(extrude);
            w.Write(isPolygon);
            w.AlignStream();
            foreach (uint i in guid)
            {
                w.Write(i);
            }
            w.Write(second);
            w.WritePrefixedList(atlasTags, a => a.WriteTo(w));
            spriteAtlas.WriteTo(w);
            texture.WriteTo(w);
            w.Write(bytesAfterTexture);
        }

        public override bool Equals(AssetData o)
        {
            if (GetType().Equals(o))
                return texture.Equals((o as SpriteAssetData).texture);
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x06;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            foreach (AssetPtr p in atlasTags)
            {
                action(p);
            }
            action(spriteAtlas);
            action(texture);
        }
    }

    public class TextAsset : AssetData
    {
        // Master Polyglot: c4dc0d059266d8d47862f46460cf8f31, 1
        // BeatSaber: 231368cb9c1d5dd43988f2a85226e7d7, 1
        public const int ClassID = 0x31;

        public string name;
        public string script;

        public TextAsset(BinaryReader reader, int _length)
        {
            name = reader.ReadAlignedString();
            script = reader.ReadAlignedString();
        }
        public override bool Equals(AssetData o)
        {
            //TODO implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0;
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.WriteAlignedString(name);
            w.WriteAlignedString(script);
        }
    }
}

using System.IO;

namespace LibSaberPatch
{
    public class AssetPtr
    {
        public int fileID;
        public ulong pathID;

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
    }

    public class MonoBehaviorAssetData : AssetData
    {
        AssetPtr gameObject; // always zero AFAIK
        int enabled;
        AssetPtr script;
        string name;
        BehaviorData data;

        public MonoBehaviorAssetData(BinaryReader reader, int length) {
            int startOffset = (int)reader.BaseStream.Position;
            gameObject = new AssetPtr(reader);
            enabled = reader.ReadInt32();
            script = new AssetPtr(reader);
            name = reader.ReadAlignedString();
            int headerLen = (int)reader.BaseStream.Position - startOffset;

            switch(script.pathID) {
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
    }

    public class AudioClipAssetData : AssetData
    {
        string name;
        int loadType;
        int channels;
        int frequency;
        int bitsPerSample;
        float length;
        bool isTracker;

        int subsoundIndex;
        bool preloadAudio;
        bool backgroundLoad;
        bool legacy3D;

        string source;
        ulong offset;
        ulong size;
        // 0 = PCM, 1 = Vorbis, 3 = MP3, ...
        int compressionFormat;

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
    }
}

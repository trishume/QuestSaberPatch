using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.AssetDataObjects
{
    public class SpriteAssetData : AssetData
    {
        public const int ClassID = 0xD5;

        public string name;
        public float[] floats;
        public uint extrude;
        public bool isPolygon;
        public byte[] guid;
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
            guid = reader.ReadBytes(16);
            second = reader.ReadInt64();
            atlasTags = reader.ReadPrefixedList(r => new AssetPtr(r));
            spriteAtlas = new AssetPtr(reader); // SpriteAtlas
            texture = new AssetPtr(reader);
            bytesAfterTexture = reader.ReadBytes((int)(_length - (reader.BaseStream.Position - start)));
        }

        public SpriteAssetData() { }

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
            w.Write(guid);
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
}

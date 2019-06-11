using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.AssetDataObjects
{
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

        public override void WriteTo(BinaryWriter w, Apk.Version v)
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
}

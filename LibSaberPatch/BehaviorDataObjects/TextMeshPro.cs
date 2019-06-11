using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class TextMeshPro : BehaviorData
    {
        // DEPRECATED
        //public const int PathID = 1065;
        // Teiko Medium SDF No Glow: PathID 57, FileID 3 (unity-builtin-extra)
        // Teiko Medium SDF: PathID 58, FileID 3
        // 0, 283
        // Quit: 130, 26 (level1, 26)
        // Skip: 131, 24 (level2, 24)


        public AssetPtr material;
        public SimpleColor color;
        public byte raycastTarget;
        public PersistentCalls cullState;
        public string text;
        public byte rightToLeft;
        public AssetPtr fontAsset;
        public byte[] remainingData;
        public TextMeshPro(BinaryReader reader, int length)
        {
            long start = reader.BaseStream.Position;
            material = new AssetPtr(reader);
            color = new SimpleColor(reader, 16);
            raycastTarget = reader.ReadByte();
            reader.AlignStream();
            cullState = new PersistentCalls(reader);
            text = reader.ReadAlignedString();
            rightToLeft = reader.ReadByte();
            reader.AlignStream();
            fontAsset = new AssetPtr(reader);
            remainingData = reader.ReadBytes(length - (int)(reader.BaseStream.Position - start));
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(material);
            color.Trace(action);
            action(fontAsset);
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            material.WriteTo(w);
            color.WriteTo(w, v);
            w.Write(raycastTarget);
            w.AlignStream();
            cullState.WriteTo(w);
            w.WriteAlignedString(text);
            w.Write(rightToLeft);
            w.AlignStream();
            fontAsset.WriteTo(w);
            w.Write(remainingData);
        }
    }
}

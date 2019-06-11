using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class LocalizedTextMeshProUGUI : BehaviorData
    {
        // Restart Button Text: 142, 129 (level11)
        // DEPRECATED!
        //public const int PathID = 345;

        public AssetPtr text;
        public byte maintainTextAlignment;
        public string key;

        public LocalizedTextMeshProUGUI(BinaryReader reader, int _length)
        {
            text = new AssetPtr(reader);
            maintainTextAlignment = reader.ReadByte();
            reader.AlignStream();
            key = reader.ReadAlignedString();
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            text.WriteTo(w);
            w.Write(maintainTextAlignment);
            w.AlignStream();
            w.WriteAlignedString(key);
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(text);
        }
    }
}

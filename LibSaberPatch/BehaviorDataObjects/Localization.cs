using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class LocalizationDocument
    {
        public string docsID;
        public string sheetID;
        public int format;
        public AssetPtr textAsset;
        public bool downloadOnStart;

        public LocalizationDocument(BinaryReader reader)
        {
            docsID = reader.ReadAlignedString();
            sheetID = reader.ReadAlignedString();
            format = reader.ReadInt32();
            textAsset = new AssetPtr(reader);
            downloadOnStart = reader.ReadBoolean();
            reader.AlignStream();
        }

        public void WriteTo(BinaryWriter w)
        {
            w.WriteAlignedString(docsID);
            w.WriteAlignedString(sheetID);
            w.Write(format);
            textAsset.WriteTo(w);
            w.Write(downloadOnStart);
            w.AlignStream();
        }

        public void Trace(Action<AssetPtr> action)
        {
            action(textAsset);
        }
    }

    public class LocalizationAsset
    {
        public AssetPtr textAsset;
        public int format;

        public LocalizationAsset(BinaryReader reader)
        {
            textAsset = new AssetPtr(reader);
            format = reader.ReadInt32();
        }

        public void WriteTo(BinaryWriter w)
        {
            textAsset.WriteTo(w);
            w.Write(format);
        }

        public void Trace(Action<AssetPtr> action)
        {
            action(textAsset);
        }
    }

    public class Localization : BehaviorData
    {
        // Localization: 4, 1 (0f74782e1b8b9d744b2e1b71fdbc68af)
        // DEPRECATED!
        //public const int PathID = 1697;

        public LocalizationDocument polyglotDocument;
        public LocalizationDocument customDocument;
        public List<LocalizationAsset> inputFiles;
        public List<int> supportedLanguages;
        public int selectedLanguage;
        public int fallbackLanguage;
        public PersistentCalls localize;

        public Localization(BinaryReader reader, int _length)
        {
            polyglotDocument = new LocalizationDocument(reader);
            customDocument = new LocalizationDocument(reader);
            inputFiles = reader.ReadPrefixedList(r => new LocalizationAsset(r));
            supportedLanguages = reader.ReadPrefixedList(r => r.ReadInt32());
            selectedLanguage = reader.ReadInt32();
            fallbackLanguage = reader.ReadInt32();
            localize = new PersistentCalls(reader);
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            polyglotDocument.WriteTo(w);
            customDocument.WriteTo(w);
            w.WritePrefixedList(inputFiles, f => f.WriteTo(w));
            w.Write(selectedLanguage);
            w.Write(fallbackLanguage);
            localize.WriteTo(w);
        }

        public override void Trace(Action<AssetPtr> action)
        {
            polyglotDocument.Trace(action);
            customDocument.Trace(action);
            foreach (LocalizationAsset a in inputFiles)
            {
                a.Trace(action);
            }
        }
    }
}

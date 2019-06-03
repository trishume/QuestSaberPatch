using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class LocalizationDocument : BehaviorData
    {
        public string docsID;
        public string sheetID;
        public int format;
        public AssetPtr textAsset;
        public bool downloadOnStart;

        public LocalizationDocument(BinaryReader reader, int _length)
        {
            docsID = reader.ReadAlignedString();
            sheetID = reader.ReadAlignedString();
            format = reader.ReadInt32();
            textAsset = new AssetPtr(reader);
            downloadOnStart = reader.ReadBoolean();
            reader.AlignStream();
        }

        public override bool Equals(BehaviorData data)
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
            w.WriteAlignedString(docsID);
            w.WriteAlignedString(sheetID);
            w.Write(format);
            textAsset.WriteTo(w);
            w.Write(downloadOnStart);
            w.AlignStream();
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(textAsset);
        }
    }

    public class LocalizationAsset : BehaviorData
    {
        public AssetPtr textAsset;
        public int format;

        public LocalizationAsset(BinaryReader reader, int _length)
        {
            textAsset = new AssetPtr(reader);
            format = reader.ReadInt32();
        }

        public override bool Equals(BehaviorData data)
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
            textAsset.WriteTo(w);
            w.Write(format);
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(textAsset);
        }
    }

    public class Localization : BehaviorData
    {
        // Localization: 4, 1 (0f74782e1b8b9d744b2e1b71fdbc68af)
        public const int PathID = 1697;

        public LocalizationDocument polyglotDocument;
        public LocalizationDocument customDocument;
        public List<LocalizationAsset> inputFiles;
        public List<int> supportedLanguages;
        public int selectedLanguage;
        public int fallbackLanguage;
        public PersistentCalls localize;

        public Localization(BinaryReader reader, int _length)
        {
            polyglotDocument = new LocalizationDocument(reader, -1); // Length unknown
            customDocument = new LocalizationDocument(reader, -1); // Length unknown
            inputFiles = reader.ReadPrefixedList(r => new LocalizationAsset(r, -1)); // Length unknown
            supportedLanguages = reader.ReadPrefixedList(r => r.ReadInt32());
            selectedLanguage = reader.ReadInt32();
            fallbackLanguage = reader.ReadInt32();
            localize = new PersistentCalls(reader, -1); // Length unknown
        }

        public override bool Equals(BehaviorData data)
        {
            //TODO implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x0F;
        }

        public override void WriteTo(BinaryWriter w)
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

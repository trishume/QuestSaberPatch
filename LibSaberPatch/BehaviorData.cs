using System;
using System.IO;
using System.Collections.Generic;

namespace LibSaberPatch
{
    public abstract class BehaviorData
    {
        public abstract void WriteTo(BinaryWriter w);
        public abstract int SharedAssetsTypeIndex();
        public abstract bool Equals(BehaviorData data);
        // Could maybe also make this method non-abstract using reflection
        /// <summary>
        /// Traces all AssetPtrs owned by this BehaviorData and calls the action on all of them.
        /// </summary>
        /// <param name="action">The action to run on each AssetPtr.</param>
        public virtual void Trace(Action<AssetPtr> action)
        {
            // Default to trace nothing
        }
        /// <summary>
        /// Returns a list of all owned files of this BehaviorData, it also checks its AssetPtrs.
        /// </summary>
        /// <param name="action">Returns a list of all owned files.</param>
        public virtual List<string> OwnedFiles(SerializedAssets assets)
        {
            // Default to return no owned files
            return new List<string>();
        }
    }

    public class UnknownBehaviorData : BehaviorData
    {
        public byte[] bytes;
        public UnknownBehaviorData(BinaryReader reader, int length) {
            bytes = reader.ReadBytes(length);
        }

        public override void WriteTo(BinaryWriter w) {
            w.Write(bytes);
        }

        public override int SharedAssetsTypeIndex() {
            throw new ApplicationException("unknown type index");
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
                return bytes.Equals((data as UnknownBehaviorData).bytes);
            return false;
        }
    }

    public class LevelCollectionBehaviorData : BehaviorData
    {
        public const int PathID = 762;

        public List<AssetPtr> levels;

        public LevelCollectionBehaviorData()
        {
            levels = new List<AssetPtr>();
        }

        public LevelCollectionBehaviorData(BinaryReader reader, int length) {
            levels = reader.ReadPrefixedList(r => new AssetPtr(r));
        }

        public override void WriteTo(BinaryWriter w) {
            w.WritePrefixedList(levels, x => x.WriteTo(w));
        }

        public override int SharedAssetsTypeIndex() {
            return 0x10;
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
                return levels.Equals((data as LevelCollectionBehaviorData).levels);
            return false;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            foreach (AssetPtr p in levels)
            {
                action(p);
            }
        }
    }

    public class LevelPackBehaviorData : BehaviorData
    {
        public const int PathID = 1480;

        public string packID;
        public string packName;
        public AssetPtr coverImage;
        public bool isPackAlwaysOwned;
        public AssetPtr beatmapLevelCollection;


        public LevelPackBehaviorData()
        {
            isPackAlwaysOwned = true;
        }
        public LevelPackBehaviorData(BinaryReader reader, int length)
        {
            packID = reader.ReadAlignedString();
            packName = reader.ReadAlignedString();
            coverImage = new AssetPtr(reader);
            isPackAlwaysOwned = Convert.ToBoolean(reader.ReadByte());
            reader.AlignStream();
            beatmapLevelCollection = new AssetPtr(reader);
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.WriteAlignedString(packID);
            w.WriteAlignedString(packName);
            coverImage.WriteTo(w);
            w.Write(isPackAlwaysOwned);
            w.AlignStream();
            beatmapLevelCollection.WriteTo(w);
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x1C;
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
                return packID == (data as LevelPackBehaviorData).packID;
            return false;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(coverImage);
            action(beatmapLevelCollection);
        }
    }

    public class BeatmapLevelPackCollection : BehaviorData
    {
        public const int PathID = 1530;

        public List<AssetPtr> beatmapLevelPacks;
        public List<AssetPtr> previewBeatmapLevelPack;

        public BeatmapLevelPackCollection(BinaryReader reader, int _length)
        {
            beatmapLevelPacks = reader.ReadPrefixedList(r => new AssetPtr(r));
            previewBeatmapLevelPack = reader.ReadPrefixedList(r => new AssetPtr(r));
        }

        public override bool Equals(BehaviorData data)
        {
            //TODO Implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x01;
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.WritePrefixedList(beatmapLevelPacks, a => a.WriteTo(w));
            w.WritePrefixedList(previewBeatmapLevelPack, a => a.WriteTo(w));
        }

        public override void Trace(Action<AssetPtr> action)
        {
            foreach (AssetPtr p in beatmapLevelPacks)
            {
                action(p);
            }
            foreach (AssetPtr p in previewBeatmapLevelPack)
            {
                action(p);
            }
        }
    }

    public class BeatmapDataBehaviorData : BehaviorData
    {
        public const int PathID = 1552;

        public string jsonData;
        public byte[] signature;
        public byte[] projectedData;

        public BeatmapDataBehaviorData() {}

        public BeatmapDataBehaviorData(BinaryReader reader, int length) {
            jsonData = reader.ReadAlignedString();
            signature = reader.ReadPrefixedBytes();
            projectedData = reader.ReadPrefixedBytes();
        }

        public override void WriteTo(BinaryWriter w) {
            w.WriteAlignedString(jsonData);
            w.WritePrefixedBytes(signature);
            w.WritePrefixedBytes(projectedData);
        }

        public override int SharedAssetsTypeIndex() {
            return 0x0E;
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
                return projectedData.Equals((data as BeatmapDataBehaviorData).projectedData);
            return false;
        }
    }

    public class BeatmapDifficulty
    {
        public int difficulty;
        public int difficultyRank;
        public float noteJumpMovementSpeed;
        public int noteJumpStartBeatOffset;
        public AssetPtr beatmapData;

        public BeatmapDifficulty() {}

        public BeatmapDifficulty(BinaryReader reader) {
            difficulty = reader.ReadInt32();
            difficultyRank = reader.ReadInt32();
            noteJumpMovementSpeed = reader.ReadSingle();
            noteJumpStartBeatOffset = reader.ReadInt32();
            beatmapData = new AssetPtr(reader);
        }

        public void WriteTo(BinaryWriter w) {
            w.Write(difficulty);
            w.Write(difficultyRank);
            w.Write(noteJumpMovementSpeed);
            w.Write(noteJumpStartBeatOffset);
            beatmapData.WriteTo(w);
        }
    }

    public class BeatmapSet
    {
        public AssetPtr characteristic;
        public List<BeatmapDifficulty> difficultyBeatmaps;

        public BeatmapSet() {}

        public BeatmapSet(BinaryReader reader) {
            characteristic = new AssetPtr(reader);
            difficultyBeatmaps = reader.ReadPrefixedList(r => new BeatmapDifficulty(r));
        }

        public void WriteTo(BinaryWriter w) {
            characteristic.WriteTo(w);
            w.WritePrefixedList(difficultyBeatmaps, x => x.WriteTo(w));
        }
    }

    public class LevelBehaviorData : BehaviorData
    {
        public const int PathID = 644;

        public string levelID;
        public string songName;
        public string songSubName;
        public string songAuthorName;
        public string levelAuthorName;
        public AssetPtr audioClip;
        public float beatsPerMinute;
        public float songTimeOffset;
        public float shuffle;
        public float shufflePeriod;
        public float previewStartTime;
        public float previewDuration;
        public AssetPtr coverImage;
        public AssetPtr environment;

        public List<BeatmapSet> difficultyBeatmapSets;

        public LevelBehaviorData() {}

        public LevelBehaviorData(BinaryReader reader, int length) {
            levelID = reader.ReadAlignedString();
            songName = reader.ReadAlignedString();
            songSubName = reader.ReadAlignedString();
            songAuthorName = reader.ReadAlignedString();
            levelAuthorName = reader.ReadAlignedString();
            audioClip = new AssetPtr(reader);
            beatsPerMinute = reader.ReadSingle();
            songTimeOffset = reader.ReadSingle();
            shuffle = reader.ReadSingle();
            shufflePeriod = reader.ReadSingle();
            previewStartTime = reader.ReadSingle();
            previewDuration = reader.ReadSingle();
            coverImage = new AssetPtr(reader);
            environment = new AssetPtr(reader);
            difficultyBeatmapSets = reader.ReadPrefixedList(r => new BeatmapSet(r));
        }

        public override void WriteTo(BinaryWriter w) {
            w.WriteAlignedString(levelID);
            w.WriteAlignedString(songName);
            w.WriteAlignedString(songSubName);
            w.WriteAlignedString(songAuthorName);
            w.WriteAlignedString(levelAuthorName);
            audioClip.WriteTo(w);
            w.Write(beatsPerMinute);
            w.Write(songTimeOffset);
            w.Write(shuffle);
            w.Write(shufflePeriod);
            w.Write(previewStartTime);
            w.Write(previewDuration);
            coverImage.WriteTo(w);
            environment.WriteTo(w);
            w.WritePrefixedList(difficultyBeatmapSets, x => x.WriteTo(w));
        }

        public override int SharedAssetsTypeIndex() {
            return 0x0F;
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
                return levelID == (data as LevelBehaviorData).levelID;
            return false;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(audioClip);
            action(coverImage);
            action(environment);
            foreach (BeatmapSet s in difficultyBeatmapSets)
            {
                action(s.characteristic);
                foreach (BeatmapDifficulty d in s.difficultyBeatmaps)
                {
                    action(d.beatmapData);
                }
            }
        }

        public override List<string> OwnedFiles(SerializedAssets assets)
        {
            var l = new List<string>();
            l.AddRange(audioClip.Follow<AudioClipAssetData>(assets).OwnedFiles(assets));
            return l;
        }
    }
    public class Saber : BehaviorData
    {
        public const int PathID = 549;

        public AssetPtr topPos;
        public AssetPtr bottomPos;
        public AssetPtr handlePos;
        public AssetPtr vrController;
        public AssetPtr saberTypeObject;

        // Here is the list of components of the Saber's GameObject:
        // Transform, VRController, BoxCollider, Rigidbody, Saber (this), SaberTypeObject, SaberModelContainer
        // MeshFilters of importance: no idea

        public Saber(BinaryReader reader, int _length)
        {
            topPos = new AssetPtr(reader);
            bottomPos = new AssetPtr(reader);
            handlePos = new AssetPtr(reader);
            vrController = new AssetPtr(reader);
            saberTypeObject = new AssetPtr(reader);
        }
        public override bool Equals(BehaviorData data)
        {
            //TODO Implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x07;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(topPos);
            action(handlePos);
            action(bottomPos);
            action(vrController);
            action(saberTypeObject);
        }

        public override void WriteTo(BinaryWriter w)
        {
            topPos.WriteTo(w);
            bottomPos.WriteTo(w);
            handlePos.WriteTo(w);
            vrController.WriteTo(w);
            saberTypeObject.WriteTo(w);
        }
    }

    public class SaberManager : BehaviorData
    {
        public const int PathID = 1210;

        public AssetPtr leftSaber;
        public AssetPtr rightSaber;

        public SaberManager(BinaryReader reader, int _length)
        {
            leftSaber = new AssetPtr(reader);
            rightSaber = new AssetPtr(reader);
        }
        public override bool Equals(BehaviorData data)
        {
            //TODO Implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0xE3;
        }

        public override void WriteTo(BinaryWriter w)
        {
            leftSaber.WriteTo(w);
            rightSaber.WriteTo(w);
        }
    }

    public class PersistentCalls : BehaviorData
    {
        public List<byte[]> calls;
        public string typeName;

        public PersistentCalls(BinaryReader reader, int _length)
        {
            // Number of bytes in calls:
            int b = 0;
            calls = reader.ReadPrefixedList(r => r.ReadBytes(b));
            typeName = reader.ReadAlignedString();
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
            w.WritePrefixedList(calls, b => w.Write(b));
            w.WriteAlignedString(typeName);
        }
    }

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

    public class LocalizedTextMeshProUGUI : BehaviorData
    {
        // Restart Button Text: 142, 129 (level11)
        public const int PathID = 345;

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
        public override bool Equals(BehaviorData data)
        {
            //TODO implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x0A;
        }

        public override void WriteTo(BinaryWriter w)
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

    public class TextMeshPro : BehaviorData
    {
        public const int PathID = 1065;
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
        public TextMeshPro(BinaryReader reader, int _length)
        {
            long start = reader.BaseStream.Position;
            material = new AssetPtr(reader);
            color = new SimpleColor(reader, 16);
            raycastTarget = reader.ReadByte();
            reader.AlignStream();
            cullState = new PersistentCalls(reader, -1); // Unknown length
            text = reader.ReadAlignedString();
            rightToLeft = reader.ReadByte();
            reader.AlignStream();
            fontAsset = new AssetPtr(reader);
            remainingData = reader.ReadBytes(_length - (int)(reader.BaseStream.Position - start));
        }
        public override bool Equals(BehaviorData data)
        {
            //TODO Implement
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0xFB;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(material);
            color.Trace(action);
            action(fontAsset);
        }

        public override void WriteTo(BinaryWriter w)
        {
            material.WriteTo(w);
            color.WriteTo(w);
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

    public class ColorManager : BehaviorData
    {
        public const int PathID = 297;

        public AssetPtr playerModel;
        public AssetPtr colorA;
        public AssetPtr colorB;

        public ColorManager(BinaryReader reader, int _length)
        {
            playerModel = new AssetPtr(reader);
            colorA = new AssetPtr(reader);
            colorB = new AssetPtr(reader);
        }

        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
            {
                var cm = data as ColorManager;
                return playerModel.Equals(cm.playerModel) && colorA.Equals(cm.colorA) && colorB.Equals(cm.colorB);
            }
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0x0E;
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(playerModel);
            action(colorA);
            action(colorB);
        }

        public override void WriteTo(BinaryWriter w)
        {
            playerModel.WriteTo(w);
            colorA.WriteTo(w);
            colorB.WriteTo(w);
        }
    }

    public class SimpleColor : BehaviorData
    {
        public const int PathID = 423;

        public float r;
        public float g;
        public float b;
        public float a;

        public SimpleColor() { }

        public SimpleColor(BinaryReader reader, int _length)
        {
            r = reader.ReadSingle();
            g = reader.ReadSingle();
            b = reader.ReadSingle();
            a = reader.ReadSingle();
        }
        public override bool Equals(BehaviorData data)
        {
            if (GetType().Equals(data))
            {
                SimpleColor c = data as SimpleColor;
                return r == c.r && g == c.g && b == c.b && a == c.a;
            }
            return false;
        }

        public override int SharedAssetsTypeIndex()
        {
            return 13;
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(r);
            w.Write(g);
            w.Write(b);
            w.Write(a);
        }
    }
}

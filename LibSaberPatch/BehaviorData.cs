using System;
using System.IO;
using System.Collections.Generic;

namespace LibSaberPatch
{
    public abstract class BehaviorData
    {
        public abstract void WriteTo(BinaryWriter w);
        public abstract int SharedAssetsTypeIndex();
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
    }

    public class LevelCollectionBehaviorData : BehaviorData
    {
        public const int PathID = 762;
        public List<AssetPtr> levels;

        public LevelCollectionBehaviorData(BinaryReader reader, int length) {
            levels = reader.ReadPrefixedList(r => new AssetPtr(r));
        }

        public override void WriteTo(BinaryWriter w) {
            w.WritePrefixedList(levels, x => x.WriteTo(w));
        }

        public override int SharedAssetsTypeIndex() {
            return 0x10;
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
    }
}

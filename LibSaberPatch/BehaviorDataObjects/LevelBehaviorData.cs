using LibSaberPatch.AssetDataObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class BeatmapDifficulty
    {
        public int difficulty;
        public int difficultyRank;
        public float noteJumpMovementSpeed;
        public int noteJumpStartBeatOffset;
        public AssetPtr beatmapData;

        public BeatmapDifficulty() { }

        public BeatmapDifficulty(BinaryReader reader)
        {
            difficulty = reader.ReadInt32();
            difficultyRank = reader.ReadInt32();
            noteJumpMovementSpeed = reader.ReadSingle();
            noteJumpStartBeatOffset = reader.ReadInt32();
            beatmapData = new AssetPtr(reader);
        }

        public void WriteTo(BinaryWriter w)
        {
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

        public BeatmapSet() { }

        public BeatmapSet(BinaryReader reader)
        {
            characteristic = new AssetPtr(reader);
            difficultyBeatmaps = reader.ReadPrefixedList(r => new BeatmapDifficulty(r));
        }

        public void WriteTo(BinaryWriter w)
        {
            characteristic.WriteTo(w);
            w.WritePrefixedList(difficultyBeatmaps, x => x.WriteTo(w));
        }
    }

    public class LevelBehaviorData : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("A3EC9046011206F5CD109314850602E3");

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

        public LevelBehaviorData() { }

        public LevelBehaviorData(BinaryReader reader, int length)
        {
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

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
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
}

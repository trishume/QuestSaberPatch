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
        public abstract void Trace(Action<AssetPtr> action);
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

        public override void Trace(Action<AssetPtr> action)
        {
            // Nothing is known, don't trace any AssetPtrs.
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

        public override void Trace(Action<AssetPtr> action)
        {
            // No AssetPtrs to trace.
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

        public ulong RemoveFromAssets(SerializedAssets assets, Apk.Transaction apk)
        {
            // We want to find the level object in the assets list of objects so that we can remove it via PathID.
            // Well, this is quite a messy solution... But it _should work_...
            // What this is doing: Removing the asset that is a monobehavior, and the monobehavior's data equals this level.
            // Then it casts that to a level behavior data.

            // TODO Make this work with Transactions instead of an assets object.

            //Console.WriteLine(assets.GetAssetAt(264).data.Equals(assets.GetAssetAt(audioClip.pathID)));

            // Also remove difficulty beatmaps
            foreach (BeatmapSet s in difficultyBeatmapSets)
            {
                foreach (BeatmapDifficulty d in s.difficultyBeatmaps)
                {
                    //Console.WriteLine($"Removing Difficulty: {d.difficulty} with characteristic PathID: {s.characteristic.pathID} with PathID: {d.beatmapData.pathID}");
                    assets.RemoveAssetAt(d.beatmapData.pathID);
                }
            }
            // Remove cover image
            assets.RemoveAssetAt(coverImage.pathID);
            // Remove the file for the audio asset and the audio clip
            Console.WriteLine(audioClip.pathID);
            Console.WriteLine(assets.objects.Count);
            if (assets.GetAssetAt(audioClip.pathID).data != null)
                Console.WriteLine($"{audioClip.pathID} has type: {assets.GetAssetAt(audioClip.pathID).data.GetType()}");
            var audioAsset = (assets.RemoveAssetAt(audioClip.pathID).data as AudioClipAssetData);
            if (audioAsset == null)
            {
                Console.WriteLine(audioClip.pathID + " not found as an AudioClip! Highest known PathID: " + assets.objects[assets.objects.Count - 1].pathID);
            }
            if (apk != null) apk.RemoveFileAt($"assets/bin/Data/{audioAsset.source}");
            // Remove itself!
            ulong levelPathID = assets.RemoveScript(this).pathID;
            return levelPathID;
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
    }
}

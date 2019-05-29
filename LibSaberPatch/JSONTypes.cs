using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LibSaberPatch
{

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Difficulty : int
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3,
        ExpertPlus = 4
    }

    public class JsonBeatmapDifficulty
    {
        public Difficulty _difficulty;
        public int _difficultyRank;
        public float _noteJumpMovementSpeed;
        public int _noteJumpStartBeatOffset;

        public string _beatmapFilename;

        public BeatmapDifficulty ToAssets(SerializedAssets assets) {
            return new BeatmapDifficulty() {
                difficulty = (int)_difficulty,
                difficultyRank = _difficultyRank,
                noteJumpMovementSpeed = _noteJumpMovementSpeed,
                noteJumpStartBeatOffset = _noteJumpStartBeatOffset,

                // TODO properly create beatmap asset
                beatmapData = new AssetPtr(0, 63) // $100 bills normal
            };
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Characteristic
    {
        Standard,
        OneSaber,
        NoArrows
    }

    public class JsonBeatmapSet
    {
        public List<JsonBeatmapDifficulty> _difficultyBeatmaps;
        public Characteristic _beatmapCharacteristicName;

        public BeatmapSet ToAssets(SerializedAssets assets) {
            var set = new BeatmapSet();
            switch (_beatmapCharacteristicName)
            {
                case Characteristic.OneSaber:
                    set.characteristic = new AssetPtr(19, 1);
                    break;
                case Characteristic.NoArrows:
                    set.characteristic = new AssetPtr(6, 1);
                    break;
                case Characteristic.Standard:
                    set.characteristic = new AssetPtr(22, 1);
                    break;
            }
            set.difficultyBeatmaps = _difficultyBeatmaps.Select(s => s.ToAssets(assets)).ToList();
            return set;
        }
    }

    public class JsonLevel
    {
        public string _songName;
        public string _songSubName;
        public string _songAuthorName;
        public string _levelAuthorName;
        public float _beatsPerMinute;
        public float _songTimeOffset;
        public float _shuffle;
        public float _shufflePeriod;
        public float _previewStartTime;
        public float _previewDuration;

        public List<JsonBeatmapSet> _difficultyBeatmapSets;

        public string _songFilename;
        public string _coverImageFilename;
        public string _environmentName;

        public void AddToAssets(SerializedAssets assets) {
            LevelBehaviorData level = new LevelBehaviorData() {
                levelID = new string(_songName.Where(c => char.IsLetter(c)).ToArray()),
                songName = _songName,
                songSubName = _songSubName,
                songAuthorName = _songAuthorName,
                levelAuthorName = _levelAuthorName,
                beatsPerMinute = _beatsPerMinute,
                songTimeOffset = _songTimeOffset,
                shuffle = _shuffle,
                shufflePeriod = _shufflePeriod,
                previewStartTime = _previewStartTime,
                previewDuration = _previewDuration,

                // TODO currently $100 bills only valid in sharedassets17
                audioClip = new AssetPtr(0, 28),
                coverImage = new AssetPtr(0, 18),
                environment = new AssetPtr(20, 1),

                difficultyBeatmapSets = _difficultyBeatmapSets.Select(s => s.ToAssets(assets)).ToList(),
            };

            MonoBehaviorAssetData monob = new MonoBehaviorAssetData() {
                script = new AssetPtr(1, LevelBehaviorData.PathID),
                name = level.levelID + "Level",
                data = level,
            };

            assets.AppendAsset(monob);
        }
    }
}

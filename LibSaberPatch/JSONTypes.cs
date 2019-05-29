using System;
using System.IO;
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

        public BeatmapDifficulty ToAssets(
            SerializedAssets assets,
            string levelFolderPath,
            string levelID,
            Characteristic characteristic
        ) {
            string beatmapFile = Path.Combine(levelFolderPath, _beatmapFilename);
            string jsonData = File.ReadAllText(beatmapFile);
            BeatmapSaveData saveData = JsonConvert.DeserializeObject<BeatmapSaveData>(jsonData);
            byte[] projectedData = saveData.SerializeToBinary();

            BeatmapDataBehaviorData beatmapData = new BeatmapDataBehaviorData() {
                jsonData = "",
                signature = new byte[128], // all zeros
                projectedData = projectedData,
            };
            string characteristicPart = ((characteristic == Characteristic.Standard) ? "" : characteristic.ToString());
            string assetName = levelID + characteristicPart + _difficulty.ToString() + "BeatmapData";
            MonoBehaviorAssetData monob = new MonoBehaviorAssetData() {
                script = new AssetPtr(1, BeatmapDataBehaviorData.PathID),
                name = assetName,
                data = beatmapData,
            };
            AssetPtr assetPtr = assets.AppendAsset(monob);

            return new BeatmapDifficulty() {
                difficulty = (int)_difficulty,
                difficultyRank = _difficultyRank,
                noteJumpMovementSpeed = _noteJumpMovementSpeed,
                noteJumpStartBeatOffset = _noteJumpStartBeatOffset,
                beatmapData = assetPtr,
            };
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Characteristic
    {
        Standard,
        OneSaber,
        NoArrows,

        // Unsupported
        Lightshow,
    }

    public class JsonBeatmapSet
    {
        public List<JsonBeatmapDifficulty> _difficultyBeatmaps;
        public Characteristic _beatmapCharacteristicName;

        public BeatmapSet ToAssets(SerializedAssets assets, string levelFolderPath, string levelID) {
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
                case Characteristic.Lightshow:
                    return null;
            }
            set.difficultyBeatmaps = _difficultyBeatmaps.Select(s => s.ToAssets(assets, levelFolderPath, levelID, _beatmapCharacteristicName)).ToList();
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

        private string levelFolderPath;

        public static JsonLevel LoadFromFolder(string levelFolderPath) {
            string infoJson = File.ReadAllText(Path.Combine(levelFolderPath, "info.dat"));
            JsonLevel level = JsonConvert.DeserializeObject<JsonLevel>(infoJson);
            level.levelFolderPath = levelFolderPath;
            return level;
        }

        public string LevelID() {
            return new string(_songName.Where(c => char.IsLetter(c)).ToArray());
        }

        public AssetPtr AddToAssets(SerializedAssets assets, Apk apk) {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            string levelID = LevelID();
            AudioClipAssetData audioClip = CreateAudioAsset(apk, levelID);
            AssetPtr audioClipPtr = assets.AppendAsset(audioClip);

            LevelBehaviorData level = new LevelBehaviorData() {
                levelID = levelID,
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

                audioClip = audioClipPtr,
                // TODO currently $100 bills only valid in sharedassets17
                coverImage = new AssetPtr(0, 18),
                // TODO switch on the various included environments
                environment = new AssetPtr(20, 1),

                difficultyBeatmapSets = _difficultyBeatmapSets.Select(s => s.ToAssets(assets, levelFolderPath, levelID)).Where(s => s!=null).ToList(),
            };

            MonoBehaviorAssetData monob = new MonoBehaviorAssetData() {
                script = new AssetPtr(1, LevelBehaviorData.PathID),
                name = level.levelID + "Level",
                data = level,
            };
            watch.Stop();
            Console.WriteLine("song: " + watch.ElapsedMilliseconds);

            return assets.AppendAsset(monob);
        }

        private AudioClipAssetData CreateAudioAsset(Apk apk, string levelID) {
            string audioClipFile = Path.Combine(levelFolderPath, _songFilename);
            string sourceFileName = levelID+".ogg";
            if(apk != null) apk.CopyFileInto(audioClipFile, $"assets/bin/Data/{sourceFileName}");
            ulong fileSize = (ulong)new FileInfo(audioClipFile).Length;
            NVorbis.VorbisReader v = new NVorbis.VorbisReader(audioClipFile);
            return new AudioClipAssetData() {
                name = levelID,
                loadType = 1,
                channels = v.Channels,
                frequency = v.SampleRate,
                bitsPerSample = 16,
                length = (Single)v.TotalTime.TotalSeconds,
                isTracker = false,
                subsoundIndex = 0,
                preloadAudio = false,
                backgroundLoad = true,
                legacy3D = true,
                compressionFormat = 1, // vorbis
                source = sourceFileName,
                offset = 0,
                size = fileSize,
            };
        }
    }
}

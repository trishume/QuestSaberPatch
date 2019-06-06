using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using LibSaberPatch.BehaviorDataObjects;
using LibSaberPatch.AssetDataObjects;

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

        public MonoBehaviorAssetData ToAssetData(Dictionary<byte[], AssetPtr> scripts, string levelFolderPath, string levelID, Characteristic characteristic) {
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
                script = scripts[BeatmapDataBehaviorData.ScriptID],
                name = assetName,
                data = beatmapData,
            };

            return monob;
        }

        public BeatmapDifficulty ToAssets(
            SerializedAssets.Transaction assets,
            MonoBehaviorAssetData data
        ) {
            AssetPtr assetPtr = assets.AppendAsset(data);

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

        public List<MonoBehaviorAssetData> ToAssetData(Dictionary<byte[], AssetPtr> scripts, string levelFolderPath, string levelID) {
            return _difficultyBeatmaps.Select(s => s.ToAssetData(scripts, levelFolderPath, levelID, _beatmapCharacteristicName)).ToList();
        }

        public BeatmapSet ToAssets(
            SerializedAssets.Transaction assets,
            List<MonoBehaviorAssetData> data
        ) {
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
            set.difficultyBeatmaps = _difficultyBeatmaps.Zip(data, (a, b) => (a, b)).Select((s) => s.Item1.ToAssets(assets, s.Item2)).ToList();
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

        public string GenerateBasicLevelID() {
            return new string(_songName.Where(c => char.IsLetter(c)).ToArray());
        }

        public List<List<MonoBehaviorAssetData>> ToAssetData(Dictionary<byte[], AssetPtr> scripts, string levelID) {
            return _difficultyBeatmapSets.Select(s => s.ToAssetData(scripts, levelFolderPath, levelID)).ToList();
        }

        public AssetPtr AddToAssets(SerializedAssets.Transaction assets, Apk.Transaction apk, string levelID, List<List<MonoBehaviorAssetData>> data) {
            // var watch = System.Diagnostics.Stopwatch.StartNew();
            AudioClipAssetData audioClip = CreateAudioAsset(apk, levelID);
            AssetPtr audioClipPtr = assets.AppendAsset(audioClip);

            string coverPath = Path.Combine(levelFolderPath, _coverImageFilename);
            Texture2DAssetData cover = Texture2DAssetData.CoverFromImageFile(coverPath, levelID);
            AssetPtr coverPtr = assets.AppendAsset(cover);

            AssetPtr environment = assets.environmentIDToPtr["DefaultEnvironment"];
            if(assets.environmentIDToPtr.ContainsKey(_environmentName)) {
                environment = assets.environmentIDToPtr[_environmentName];
            }


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
                coverImage = coverPtr,
                environment = environment,

                difficultyBeatmapSets = _difficultyBeatmapSets.Zip(data, (a, b) => (a, b)).Select(s => s.Item1.ToAssets(assets, s.Item2)).Where(s => s!=null).ToList(),
            };

            MonoBehaviorAssetData monob = new MonoBehaviorAssetData() {
                script = assets.scriptIDToScriptPtr[LevelBehaviorData.ScriptID],
                name = level.levelID + "Level",
                data = level,
            };
            // watch.Stop();
            // Console.WriteLine("song: " + watch.ElapsedMilliseconds);

            return assets.AppendAsset(monob);
        }

        private AudioClipAssetData CreateAudioAsset(Apk.Transaction apk, string levelID) {
            string audioClipFile = Path.Combine(levelFolderPath, _songFilename);
            string sourceFileName = levelID+".ogg";
            apk.CopyFileInto(audioClipFile, $"assets/bin/Data/{sourceFileName}");
            ulong fileSize = (ulong)new FileInfo(audioClipFile).Length;
            using (NVorbis.VorbisReader v = new NVorbis.VorbisReader(audioClipFile)) {
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
}

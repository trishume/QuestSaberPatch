using LibSaberPatch.BehaviorDataObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.AssetDataObjects
{
    public class MonoBehaviorAssetData : AssetData
    {
        public const int ClassID = 114;

        public AssetPtr gameObject; // always zero AFAIK
        public int enabled;
        public AssetPtr script;
        public string name;
        public BehaviorData data;

        public MonoBehaviorAssetData()
        {
            gameObject = new AssetPtr(0, 0);
            enabled = 1;
        }

        public MonoBehaviorAssetData(BinaryReader reader, int length)
        {
            int startOffset = (int)reader.BaseStream.Position;
            gameObject = new AssetPtr(reader);
            enabled = reader.ReadInt32();
            script = new AssetPtr(reader);
            name = reader.ReadAlignedString();
            int headerLen = (int)reader.BaseStream.Position - startOffset;

            switch (script.pathID)
            {
                case LevelBehaviorData.PathID:
                    data = new LevelBehaviorData(reader, length - headerLen);
                    break;
                case LevelCollectionBehaviorData.PathID:
                    data = new LevelCollectionBehaviorData(reader, length - headerLen);
                    break;
                case BeatmapDataBehaviorData.PathID:
                    data = new BeatmapDataBehaviorData(reader, length - headerLen);
                    break;
                case LevelPackBehaviorData.PathID:
                    data = new LevelPackBehaviorData(reader, length - headerLen);
                    break;
                case ColorManager.PathID:
                    data = new ColorManager(reader, length - headerLen);
                    break;
                case SimpleColor.PathID:
                    data = new SimpleColor(reader, length - headerLen);
                    break;
                case TextMeshPro.PathID:
                    data = new TextMeshPro(reader, length - headerLen);
                    break;
                case Saber.PathID:
                    data = new Saber(reader, length - headerLen);
                    break;
                case BeatmapLevelPackCollection.PathID:
                    data = new BeatmapLevelPackCollection(reader, length - headerLen);
                    break;
                case LocalizedTextMeshProUGUI.PathID:
                    data = new LocalizedTextMeshProUGUI(reader, length - headerLen);
                    break;
                case Localization.PathID:
                    data = new Localization(reader, length - headerLen);
                    break;
                default:
                    data = new UnknownBehaviorData(reader, length - headerLen);
                    break;
            }
        }

        public override void WriteTo(BinaryWriter w)
        {
            gameObject.WriteTo(w);
            w.Write(enabled);
            script.WriteTo(w);
            w.WriteAlignedString(name);
            data.WriteTo(w);
        }

        public override int SharedAssetsTypeIndex()
        {
            return data.SharedAssetsTypeIndex();
        }

        public override void Trace(Action<AssetPtr> action)
        {
            // So, we have AssetPtrs, however, we don't want to delete any of the pointers for Gameobject/Script
            // We DO want to delete/call trace on all pointers that are in data.
            data.Trace(action);
        }

        public override List<string> OwnedFiles(SerializedAssets assets)
        {
            return data.OwnedFiles(assets);
        }
    }
}

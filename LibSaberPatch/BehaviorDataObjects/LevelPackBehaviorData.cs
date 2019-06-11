using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibSaberPatch.AssetDataObjects;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class CustomPackInfo {
        public AssetPtr pack;
        public AssetPtr collection;
    }

    public class LevelPackBehaviorData : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("8F442E25C9A4AAC8DABEC88917B0DC7D");

        public string packID;
        public string packName;
        public AssetPtr coverImage;
        public bool isPackAlwaysOwned;
        public AssetPtr beatmapLevelCollection;

        public LevelPackBehaviorData()
        {
            isPackAlwaysOwned = true;
        }
        public LevelPackBehaviorData(BinaryReader reader, int length, Apk.Version v)
        {
            packID = reader.ReadAlignedString();
            packName = reader.ReadAlignedString();
            coverImage = new AssetPtr(reader);
            if(v < Apk.Version.V1_1_0) isPackAlwaysOwned = Convert.ToBoolean(reader.ReadByte());
            reader.AlignStream();
            beatmapLevelCollection = new AssetPtr(reader);
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WriteAlignedString(packID);
            w.WriteAlignedString(packName);
            coverImage.WriteTo(w);
            if(v < Apk.Version.V1_1_0) w.Write(isPackAlwaysOwned);
            w.AlignStream();
            beatmapLevelCollection.WriteTo(w);
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(coverImage);
            action(beatmapLevelCollection);
        }

        public static CustomPackInfo CreateCustomPack(
            SerializedAssets.Transaction assets,
            string id,
            string name,
            string coverImagePath
        )
        {
            CustomPackInfo res = new CustomPackInfo();

            var texture = Texture2DAssetData.CoverFromImageFile(coverImagePath, id, true);
            var texturePtr = assets.AppendAsset(texture);
            var sprite = SpriteAssetData.CreateCoverSprite(assets, texturePtr, id);
            var spPtr = assets.AppendAsset(sprite);

            res.collection = assets.AppendAsset(new MonoBehaviorAssetData() {
                data = new LevelCollectionBehaviorData(),
                name = id + "Collection",
                script = assets.scriptIDToScriptPtr[LevelCollectionBehaviorData.ScriptID]
            });

            res.pack = assets.AppendAsset(new MonoBehaviorAssetData() {
                data = new LevelPackBehaviorData() {
                    packName = name,
                    packID = id + "Pack",
                    isPackAlwaysOwned = true,
                    beatmapLevelCollection = res.collection,
                    coverImage = spPtr,
                },
                name = id + "Pack",
                script = assets.scriptIDToScriptPtr[LevelPackBehaviorData.ScriptID]
            });

            return res;
        }

        // only works on custom packs created by CreateCustomPack that are at the end of the file
        public static void RemoveCustomPacksFromEnd(SerializedAssets assets) {
            var objs = assets.objects;
            int toRemove = 0;
            for(int i = objs.Count-1; i >= 3; i -= 4) {
                if(!(objs[i].data.isScript<LevelPackBehaviorData>())) break;
                if(!(objs[i-1].data.isScript<LevelCollectionBehaviorData>())) break;
                if(!(objs[i-2].data is SpriteAssetData)) break;
                if(!(objs[i-3].data is Texture2DAssetData)) break;
                toRemove += 4;
            }
            if(toRemove > 0) {
                objs.RemoveRange(objs.Count - toRemove, toRemove);
            }
        }
    }
}

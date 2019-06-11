using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibSaberPatch.AssetDataObjects;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class ColorManager : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("A2996891087D7B9FEEF00137BF8AE624");

        public AssetPtr playerModel;
        public AssetPtr colorA;
        public AssetPtr colorB;

        public ColorManager(BinaryReader reader, int _length)
        {
            playerModel = new AssetPtr(reader);
            colorA = new AssetPtr(reader);
            colorB = new AssetPtr(reader);
        }

        public override void Trace(Action<AssetPtr> action)
        {
            action(playerModel);
            action(colorA);
            action(colorB);
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            playerModel.WriteTo(w);
            colorA.WriteTo(w);
            colorB.WriteTo(w);
        }

        public enum ColorSide {
            A,
            B
        }

        public void UpdateColor(SerializedAssets assets, SimpleColor c, ColorSide side) {
            // Reset if null
            if(c == null) {
                if(side == ColorSide.A) {
                    c = SimpleColor.DefaultColorA();
                } else {
                    c = SimpleColor.DefaultColorB();
                }
            }

            if(side == ColorSide.A) {
                colorA.Follow<MonoBehaviorAssetData>(assets).data = c;
            } else {
                colorB.Follow<MonoBehaviorAssetData>(assets).data = c;
            }
        }
    }
}

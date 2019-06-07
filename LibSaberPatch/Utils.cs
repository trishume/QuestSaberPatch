using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using LibSaberPatch.BehaviorDataObjects;
using LibSaberPatch.AssetDataObjects;

namespace LibSaberPatch
{
    public static class Utils {
        public static void FindLevels(string startDir, Action<string> del) {
            string infoPath = Path.Combine(startDir, "info.dat");
            if(File.Exists(infoPath)) {
                del(startDir);
            } else {
                foreach (string d in Directory.GetDirectories(startDir)) {
                    FindLevels(d, del);
                }
            }
        }

        public static byte[] ImageFileToMipData(string imagePath, int topDim) {
            // pre-compute size of all mips together
            int totalSize = 0;
            for(int dim = topDim; dim > 0; dim /= 2) {
                totalSize += dim*dim;
            }
            totalSize *= 3; // 3 bytes per pixel

            byte[] imageData = new byte[totalSize];
            Span<byte> imageDataSpan = imageData;
            using (Image<Rgb24> image = Image.Load<Rgb24>(Configuration.Default, imagePath)) {
                int dataWriteIndex = 0;
                for(int dim = topDim; dim > 0; dim /= 2) {
                    image.Mutate(x => x.Resize(dim, dim));
                    for (int y = 0; y < image.Height; y++) {
                        // need to do rows in reverse order to match what Unity wants
                        Span<Rgb24> rowPixels = image.GetPixelRowSpan((image.Height-1)-y);
                        Span<byte> rowData = MemoryMarshal.AsBytes(rowPixels);
                        rowData.CopyTo(imageDataSpan.Slice(dataWriteIndex, rowData.Length));
                        dataWriteIndex += rowData.Length;
                    }
                }
            }
            return imageData;
        }

        // https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
        public static byte[] HexToBytes(string hex) {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        // This method will fail if given an object that is not a LevelBehaviorData
        public static void RemoveLevel(SerializedAssets assets, SerializedAssets.AssetObject obj, Apk.Transaction apk)
        {
            LevelBehaviorData level = (obj.data as MonoBehaviorAssetData).data as LevelBehaviorData;

            // Remove audio file
            foreach (string s in level.OwnedFiles(assets)) {
                apk.RemoveFileAt($"assets/bin/Data/{s}");
            }

            // Remove things from bottom up, so that pointers to other assets still are rooted
            // and so get fixed up by RemoveAssetAt

            foreach (BeatmapSet s in level.difficultyBeatmapSets) {
                foreach (BeatmapDifficulty d in s.difficultyBeatmaps) {
                    assets.RemoveAssetAt(d.beatmapData.pathID);
                }
            }

            assets.RemoveAssetAt(level.coverImage.pathID);
            assets.RemoveAssetAt(level.audioClip.pathID);

            assets.RemoveAssetAt(obj.pathID);
        }

        public static ColorManager CreateColor(SerializedAssets assets, SimpleColor c, bool left)
        {
            Console.WriteLine($"Creating CustomColor with r: {c.r} g: {c.g} b: {c.b} a: {c.a}");

            var dat = assets.FindScript<ColorManager>(cm => true); // Should only have one color manager

            var mbl = new MonoBehaviorAssetData()
            {
                name = "LeftCustomColor",
                data = c,
                script = assets.scriptIDToScriptPtr[SimpleColor.ScriptID]
            };
            var mbr = new MonoBehaviorAssetData()
            {
                name = "RightCustomColor",
                data = c,
                script = assets.scriptIDToScriptPtr[SimpleColor.ScriptID]
            };
            //var dat = ((MonoBehaviorAssetData)assets.GetAssetAt(52).data).data as ColorManager;
            if (dat.colorA.pathID != 54)
            {
                if (!left)
                {
                    Console.WriteLine($"Replaced existing CustomColor at PathID: {dat.colorA.pathID}");
                    assets.SetAssetAt(dat.colorA.pathID, mbl);
                }
            }
            else if (!left)
            {
                dat.colorA = assets.AppendAsset(mbl);
                Console.WriteLine($"Created new CustomColor at PathID: {dat.colorA.pathID}");
            }

            if (dat.colorB.pathID != 53)
            {
                if (left)
                {
                    Console.WriteLine($"Replaced existing CustomColor at PathID: {dat.colorB.pathID}");
                    assets.SetAssetAt(dat.colorB.pathID, mbr);
                }
            }
            else if (left)
            {
                dat.colorB = assets.AppendAsset(mbr);
                Console.WriteLine($"Created new CustomColor at PathID: {dat.colorB.pathID}");
            }
            return dat;
        }

        public static void ResetColors(SerializedAssets assets)
        {
            ColorManager manager = assets.FindScript<ColorManager>(cm => true); // Should only have one color manager
            if (manager.colorA.pathID != 54)
            {
                Console.WriteLine($"Removing CustomColor at PathID: {manager.colorA.pathID}");
                assets.RemoveAssetAt(manager.colorA.pathID);
                manager.colorA.pathID = 54;
            }
            if (manager.colorB.pathID != 53)
            {
                Console.WriteLine($"Removing CustomColor at PathID: {manager.colorB.pathID}");
                assets.RemoveAssetAt(manager.colorB.pathID);
                manager.colorB.pathID = 53;
            }
        }

        public static Dictionary<string, List<string>> ReadLocaleText(string text, List<char> seps)
        {
            //string keyName = "STRING ID";
            //string descName = "DESCRIPTION";
            //string valueName = "ENGLISH";
            //string nextName = "LANGUAGE_CODE";
            //List<char> seps = new List<char>();
            //seps.Add(text[keyName.Length]);
            //seps.Add(text[text.IndexOf(valueName) - 1]);
            //seps.Add(text[text.IndexOf(nextName) - 1]);

            var segments = new List<string>();

            string temp = "";
            bool quote = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (seps.Contains(text[i]) && !quote)
                {
                    // Seperator. Let us separate
                    segments.Add(temp);
                    temp = "";
                    continue;
                }
                temp += text[i];
                if (text[i] == '"')
                {
                    quote = !quote;
                }
            }
            segments.Add(temp);
            Dictionary<string, List<string>> o = new Dictionary<string, List<string>>();
            for (int i = 0; i < segments.Count - seps.Count + 1; i += seps.Count)
            {
                List<string> segs = new List<string>();
                for (int j = 1; j < seps.Count; j++)
                {
                    segs.Add(segments[i + j]);
                }
                o.Add(segments[i], segs);
            }
            return o;
        }

        public static void ApplyWatermark(Dictionary<string, List<string>> localeValues)
        {
            string header = "\n<size=150%><color=#EC1C24FF>Quest Modders</color></size>";
            string testersHeader = "<color=#E543E5FF>Testers</color>";

            string sc2ad = "<color=#EDCE21FF>Sc2ad</color>";
            string trishume = "<color=#40E0D0FF>trishume</color>";
            string emulamer = "<color=#00FF00FF>emulamer</color>";
            string jakibaki = "<color=#4268F4FF>jakibaki</color>";
            string elliotttate = "<color=#67AAFBFF>elliotttate</color>";
            string leo60228 = "<color=#00FF00FF>leo60228</color>";
            string trueavid = "<color=#FF8897FF>Trueavid</color>";
            string kayTH = "<color=#40FE97FF>kayTH</color>";

            string message = '\n' + header + '\n' + sc2ad + '\n' + trishume + '\n' + emulamer + '\n' + jakibaki +
                '\n' + elliotttate + '\n' + leo60228 + '\n' + testersHeader + '\n' + trueavid + '\n' + kayTH;

            var value = localeValues["CREDITS_CONTENT"];
            string item = value[value.Count - 1];
            if (item.Contains(message)) return;
            localeValues["CREDITS_CONTENT"][value.Count - 1] = item.Remove(item.Length - 2) + message + '"';
        }

        public static string WriteLocaleText(Dictionary<string, List<string>> values, List<char> seps)
        {
            string temp = "";
            foreach (string s in values.Keys)
            {
                temp += s + seps[0];
                for (int i = 1; i < seps.Count; i++)
                {
                    temp += values[s][i - 1];
                    temp += seps[i];
                }
            }
            temp = temp.Remove(temp.Length - 1);
            return temp;
        }
    }

    // loosely based on https://stackoverflow.com/questions/1440392/use-byte-as-key-in-dictionary
    public class ByteArrayComparer : EqualityComparer<byte[]> {
        public override bool Equals(byte[] first, byte[] second) {
            if (first == null || second == null) {
                return first == second;
            }
            return first.SequenceEqual(second);
        }

        public override int GetHashCode(byte[] obj) {
            if (obj.Length >= 4) {
                return BitConverter.ToInt32(obj, 0);
            }
            return 0;
        }

    }
}

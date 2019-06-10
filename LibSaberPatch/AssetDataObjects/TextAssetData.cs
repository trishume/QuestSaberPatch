using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.AssetDataObjects
{
    public class TextAssetData : AssetData
    {
        // Master Polyglot: c4dc0d059266d8d47862f46460cf8f31, 1
        // BeatSaber: 231368cb9c1d5dd43988f2a85226e7d7, 1
        public const int ClassID = 0x31;

        public string name;
        public string script;

        public TextAssetData(BinaryReader reader, int _length)
        {
            name = reader.ReadAlignedString();
            script = reader.ReadAlignedString();
        }

        public override int SharedAssetsTypeIndex()
        {
            return 0;
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WriteAlignedString(name);
            w.WriteAlignedString(script);
        }

        public Dictionary<string, List<string>> ReadLocaleText(List<char> seps)
        {
            var segments = new List<string>();

            string temp = "";
            bool quote = false;
            for (int i = 0; i < script.Length; i++)
            {
                if (seps.Contains(script[i]) && !quote)
                {
                    // Seperator. Let us separate
                    segments.Add(temp);
                    temp = "";
                    continue;
                }
                temp += script[i];
                if (script[i] == '"')
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
            if (item.Contains("Quest Modders")) return;
            localeValues["CREDITS_CONTENT"][value.Count - 1] = item.Remove(item.Length - 2) + message + '"';
        }

        public void WriteLocaleText(Dictionary<string, List<string>> values, List<char> seps)
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
            script = temp;
        }
    }
}

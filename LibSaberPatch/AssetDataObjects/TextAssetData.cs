using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.WriteAlignedString(name);
            w.WriteAlignedString(script);
        }

        public Dictionary<string, Dictionary<string, string>> ReadLocaleText()
        {
            // No longer supports modifying descriptions. Maybe I'll add this back in some other time
            // Assume the first line is as follows, ALWAYS: STRING ID,DESCRIPTION,LANGUAGE1,LANGUAGE 2 / 2ALIAS, ... \r\n
            var seps = new List<char>();
            var headerList = new List<string>();
            // Read header
            string header = script.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
            for (int i = 0; i < header.Count(c => c == ','); i++)
            {
                seps.Add(',');
            }
            seps.Add('\n');
            headerList.AddRange(header.Split(',').ToList());
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
            var o = new Dictionary<string, Dictionary<string, string>>();


            for (int i = 0; i < segments.Count - seps.Count + 1; i += headerList.Count)
            {
                var languageDict = new Dictionary<string, string>();
                for (int j = 0; j < headerList.Count; j++)
                {
                    languageDict.Add(headerList[j], segments[i + j]);
                }
                o.Add(segments[i], languageDict);
            }
            return o;
        }

        public static void ApplyWatermark(Dictionary<string, Dictionary<string, string>> localeValues)
        {
            // FOR NOW, ONLY APPLIES THE WATERMARK IN ENGLISH
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
            string item = value["ENGLISH"];
            if (item.Contains(message)) return;
            localeValues["CREDITS_CONTENT"]["ENGLISH"] = item.Remove(item.Length - 2) + message + '"';
        }

        public string WriteLocaleText(Dictionary<string, Dictionary<string, string>> values)
        {
            string temp = "";
            foreach (string s in values.Keys)
            {
                temp += s;
                foreach (string lang in values[s].Keys)
                {
                    temp += ',' + lang + ',' + values[s][lang];
                }
                temp += '\n';
            }
            temp = temp.Remove(temp.Length - 1);
            return temp;
        }
    }
}

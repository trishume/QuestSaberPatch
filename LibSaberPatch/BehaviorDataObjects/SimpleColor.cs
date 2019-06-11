using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch.BehaviorDataObjects
{
    public class SimpleColor : BehaviorData
    {
        public static byte[] ScriptID = Utils.HexToBytes("2A0A32BC8678D13C59FA6DF042711134");

        public float r;
        public float g;
        public float b;
        public float a;

        public SimpleColor() { }

        public SimpleColor(BinaryReader reader, int _length)
        {
            r = reader.ReadSingle();
            g = reader.ReadSingle();
            b = reader.ReadSingle();
            a = reader.ReadSingle();
        }

        public override void WriteTo(BinaryWriter w, Apk.Version v)
        {
            w.Write(r);
            w.Write(g);
            w.Write(b);
            w.Write(a);
        }

        // default is red and on the left
        public static SimpleColor DefaultColorA() {
            return new SimpleColor() {
                r = 240.0f/255.0f,
                g = 48.0f/255.0f,
                b = 48.0f/255.0f,
                a = 1.0f,
            };
        }


        // default is blue on the right
        public static SimpleColor DefaultColorB() {
            return new SimpleColor() {
                r = 48.0f/255.0f,
                g = 158.0f/255.0f,
                b = 1.0f,
                a = 1.0f,
            };
        }
    }
}

// Based on https://github.com/Perfare/AssetStudio/blob/master/AssetStudio/Extensions/BinaryWriterExtensions.cs
// Used under MIT License https://github.com/Perfare/AssetStudio/blob/master/License.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch
{
    public static class BinaryWriterExtensions
    {
        public static void AlignStream(this BinaryWriter writer, int alignment)
        {
            var pos = writer.BaseStream.Position;
            var mod = pos % alignment;
            if (mod != 0)
            {
                writer.Write(new byte[alignment - mod]);
            }
        }

        public static void AlignStream(this BinaryWriter writer)
        {
            writer.AlignStream(4);
        }

        public static void WriteAlignedString(this BinaryWriter writer, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            writer.AlignStream(4);
        }

        public static void WriteCString(this BinaryWriter writer, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes);
            writer.Write((byte)0);
        }

        public static void WriteInt32BE(this BinaryWriter writer, int i)
        {
            byte[] buff = BitConverter.GetBytes(i);
            Array.Reverse(buff);
            writer.Write(buff);
        }

        public static void WriteZeros(this BinaryWriter writer, int count)
        {
            // Dunno if there's a faster way to do this
            for(int i = 0; i < count; i++) writer.Write((byte)0);
        }

        public static void WritePrefixedBytes(this BinaryWriter w, byte[] l) {
            w.Write((int)l.Length);
            w.Write(l);
            w.AlignStream();
        }

        public static void WritePrefixedList<T>(this BinaryWriter w, List<T> l, Action<T> del) {
            w.Write((int)l.Count);
            for(int idx = 0; idx < l.Count; ++idx) {
                del(l[idx]);
            }
        }
    }
}

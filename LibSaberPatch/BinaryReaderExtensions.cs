// Based on https://github.com/Perfare/AssetStudio/blob/master/AssetStudio/Extensions/BinaryReaderExtensions.cs
// Used under MIT License https://github.com/Perfare/AssetStudio/blob/master/License.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibSaberPatch
{
    public static class BinaryReaderExtensions
    {
        public static int ReadInt32BE(this BinaryReader reader)
        {
            var buff = reader.ReadBytes(4);
            Array.Reverse(buff);
            return BitConverter.ToInt32(buff, 0);
        }

        public static void AlignStream(this BinaryReader reader)
        {
            reader.AlignStream(4);
        }

        public static void AlignStream(this BinaryReader reader, int alignment)
        {
            var pos = reader.BaseStream.Position;
            var mod = pos % alignment;
            if (mod != 0)
            {
                reader.BaseStream.Position += alignment - mod;
            }
        }

        public static string ReadAlignedString(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length > 0 && length <= reader.BaseStream.Length - reader.BaseStream.Position)
            {
                var stringData = reader.ReadBytes(length);
                var result = Encoding.UTF8.GetString(stringData);
                reader.AlignStream(4);
                return result;
            }
            return "";
        }

        public static string ReadStringToNull(this BinaryReader reader, int maxLength = 32767)
        {
            var bytes = new List<byte>();
            int count = 0;
            while (reader.BaseStream.Position != reader.BaseStream.Length && count < maxLength)
            {
                var b = reader.ReadByte();
                if (b == 0)
                {
                    break;
                }
                bytes.Add(b);
                count++;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static byte[] ReadPrefixedBytes(this BinaryReader reader) {
            int length = reader.ReadInt32();
            var bytes = reader.ReadBytes(length);
            reader.AlignStream();
            return bytes;
        }

        public static List<T> ReadPrefixedList<T>(this BinaryReader reader, Func<BinaryReader, T> del) {
            int length = reader.ReadInt32();
            var list = new List<T>(length);
            for(int i = 0; i < length; i++) {
                list.Add(del(reader));
            }
            return list;
        }

        public static bool ReadAllZeros(this BinaryReader reader, int len) {
            byte[] padding = reader.ReadBytes(len);
            foreach(byte b in padding) {
                if(b != 0) return false;
            }
            return true;
        }
    }
}

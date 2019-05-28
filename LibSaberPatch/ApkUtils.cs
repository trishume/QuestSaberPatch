using System;
using System.IO;
using System.IO.Compression;

namespace LibSaberPatch
{
    public static class ApkUtils
    {
        public static byte[] ReadEntireEntry(ZipArchive archive, string entryPath) {
            ZipArchiveEntry entry = archive.GetEntry(entryPath);
            if(entry == null) return null;
            byte[] buf = new byte[entry.Length];
            using (Stream stream = entry.Open()) {
                stream.Read(buf, 0, buf.Length);
            }
            return buf;
        }

        public static void WriteEntireEntry(ZipArchive archive, string entryPath, byte[] contents) {
            ZipArchiveEntry entry = archive.GetEntry(entryPath);
            if(entry == null) throw new FileNotFoundException(entryPath);
            using (Stream stream = entry.Open()) {
                stream.Write(contents, 0, contents.Length);
            }
        }

        public static byte[] JoinedContents(ZipArchive archive, string basePath) {
            using (MemoryStream stream = new MemoryStream()) {
                string splitBase = basePath + ".split";
                for(int i = 0; ; i++) {
                    ZipArchiveEntry entry = archive.GetEntry(splitBase+i);
                    if(entry == null && i == 0) return null;
                    if(entry == null) break;
                    using (Stream fileStream = entry.Open()) {
                        fileStream.CopyTo(stream);
                    }
                }
                stream.Close();
                return stream.ToArray();
            }
        }

        private const string il2cppLibEntry = "lib/armeabi-v7a/libil2cpp.so";
        private const int sigPatchLoc = 0x0109D074;
        public static void PatchSignatureCheck(ZipArchive archive) {
            byte[] sigPatch = {0x01, 0x00, 0xA0, 0xE3};
            byte[] data = ApkUtils.ReadEntireEntry(archive, il2cppLibEntry);
            for(int i = 0; i < sigPatch.Length; i++) {
                data[sigPatchLoc + i] = sigPatch[i];
            }
            ApkUtils.WriteEntireEntry(archive, il2cppLibEntry, data);
        }
    }
}

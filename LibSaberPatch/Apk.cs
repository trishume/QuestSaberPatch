using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace LibSaberPatch
{
    public class Apk : IDisposable
    {
        public static string MainAssetsFile = "assets/bin/Data/sharedassets17.assets";
        public static string RootPackFile = "assets/bin/Data/sharedassets19.assets";

        private ZipArchive archive;

        public Apk(string path) {
            archive = ZipFile.Open(path, ZipArchiveMode.Update);
        }

        public void Dispose() {
            archive.Dispose();
        }

        public byte[] ReadEntireEntry(string entryPath) {
            ZipArchiveEntry entry = archive.GetEntry(entryPath);
            if(entry == null) return JoinedContents(entryPath);
            byte[] buf = new byte[entry.Length];
            using (Stream stream = entry.Open()) {
                stream.Read(buf, 0, buf.Length);
            }
            return buf;
        }

        public void WriteEntireEntry(string entryPath, byte[] contents) {
            ZipArchiveEntry entry = archive.GetEntry(entryPath);
            if(entry == null) throw new FileNotFoundException(entryPath);
            using (Stream stream = entry.Open()) {
                stream.Write(contents, 0, contents.Length);
            }
        }

        public void ReplaceAssetsFile(string entryPath, byte[] contents) {
            DeleteSplits(entryPath);
            try {
                WriteEntireEntry(entryPath, contents);
            } catch(FileNotFoundException) {
                // no compression because faster and it's mostly already compressed beatmap data
                ZipArchiveEntry entry = archive.CreateEntry(entryPath, CompressionLevel.NoCompression);
                using (Stream stream = entry.Open()) {
                    stream.Write(contents, 0, contents.Length);
                }
            }
        }

        public void CopyFileInto(string sourceFilePath, string destEntryPath) {
            // this is used for pre-compressed things like songs so no compression is best
            ZipArchiveEntry entry = archive.CreateEntry(destEntryPath, CompressionLevel.NoCompression);
            using (Stream destStream = entry.Open()) {
                using (Stream fileStream = new FileStream(sourceFilePath, FileMode.Open)) {
                    fileStream.CopyTo(destStream);
                }
            }
        }

        /// <summary>
        /// Deletes the given file from the APK
        /// </summary>
        /// <param name="filePath">The file to delete in the APK</param>
        public void RemoveFileAt(string filePath)
        {
            ZipArchiveEntry entry = archive.GetEntry(filePath);
            if (entry != null) entry.Delete();
        }

        public byte[] JoinedContents(string basePath) {
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

        public void DeleteSplits(string basePath) {
            string splitBase = basePath + ".split";
            for(int i = 0; ; i++) {
                ZipArchiveEntry entry = archive.GetEntry(splitBase+i);
                if(entry == null) break;
                entry.Delete();
            }
        }

        private bool tryPatch(byte[] data, int sigPatchLoc, byte[] sigPatch, byte[] toReplace) {
            if(bytesEqualAtOffset(data, sigPatch, sigPatchLoc)) return false;
            if(!bytesEqualAtOffset(data, toReplace, sigPatchLoc))
                throw new ApplicationException("Trying to patch different version of code");

            for(int i = 0; i < sigPatch.Length; i++) {
                data[sigPatchLoc + i] = sigPatch[i];
            }
            return true;
        }

        private const string il2cppLibEntry = "lib/armeabi-v7a/libil2cpp.so";
        public void PatchSignatureCheck() {
            byte[] data = ReadEntireEntry(il2cppLibEntry);

            bool patched = false;
            byte[] sigPatch = {0x01, 0x00, 0xA0, 0xE3};
            if(data.Length == 26901596 && data[100] == 0x54) { // v1.0.1
                int sigPatchLoc = 0x13b0934;
                byte[] toReplace = {0x1A, 0x83, 0xC3, 0xEB};
                patched = tryPatch(data, sigPatchLoc, sigPatch, toReplace);
            } else if(data.Length == 26901596 && data[100] == 0x94) { // v1.0.2
                int sigPatchLoc = 0x10014B8;
                byte[] toReplace = {0x00, 0x00, 0xA0, 0xE3};
                patched = tryPatch(data, sigPatchLoc, sigPatch, toReplace);
            } else if(data.Length == 27041992) { // v1.0.0
                int sigPatchLoc = 0x0109D074;
                byte[] toReplace = {0x8B, 0xD8, 0xFE, 0xEB};
                patched = tryPatch(data, sigPatchLoc, sigPatch, toReplace);
            }

            if(patched) WriteEntireEntry(il2cppLibEntry, data);
        }

        private static bool bytesEqualAtOffset(byte[] data, byte[] patch, int offset) {
            for(int i = 0; i < patch.Length; i++) {
                if(data[offset + i] != patch[i]) return false;
            }
            return true;
        }

        public class Transaction {
            List<(string, string)> copies;
            List<string> deletions;

            public Transaction() {
                copies = new List<(string,string)>();
                deletions = new List<string>();
            }

            public void CopyFileInto(string sourceFilePath, string destEntryPath) {
                // check that we can open the source file for reading
                using (Stream fileStream = new FileStream(sourceFilePath, FileMode.Open)) {
                }
                copies.Add((sourceFilePath, destEntryPath));
            }

            /// <summary>
            /// Deletes the given file from the APK.
            /// </summary>
            /// <param name="filePath">The file to delete in the APK</param>
            public void RemoveFileAt(string filePath)
            {
                deletions.Add(filePath);
            }

            public void ApplyTo(Apk apk) {

                foreach(var copy in copies) {
                    apk.CopyFileInto(copy.Item1, copy.Item2);
                }
                foreach(string item in deletions)
                {
                    apk.RemoveFileAt(item);
                }
            }
        }
    }
}

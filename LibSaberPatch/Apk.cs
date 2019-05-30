using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace LibSaberPatch
{
    public class Apk : IDisposable
    {
        public static string MainAssetsFile = "assets/bin/Data/sharedassets17.assets";

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
        /// Deletes the given file from the APK, ASSUMING IT EXISTS!
        /// </summary>
        /// <param name="filePath">The file to delete in the APK</param>
        public void RemoveFileAt(string filePath)
        {
            ZipArchiveEntry entry = archive.GetEntry(filePath);
            entry.Delete();
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

        private const string il2cppLibEntry = "lib/armeabi-v7a/libil2cpp.so";
        private const int sigPatchLoc = 0x0109D074;
        public void PatchSignatureCheck() {
            byte[] sigPatch = {0x01, 0x00, 0xA0, 0xE3};
            byte[] data = ReadEntireEntry(il2cppLibEntry);
            for(int i = 0; i < sigPatch.Length; i++) {
                data[sigPatchLoc + i] = sigPatch[i];
            }
            WriteEntireEntry(il2cppLibEntry, data);
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
            /// Deletes the given file from the APK, ASSUMING IT EXISTS!
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

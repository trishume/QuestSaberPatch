using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using Ionic.Zip;
using Ionic.Zlib;

namespace LibSaberPatch
{
    public class Apk : IDisposable
    {
        private class FileConstants
        {
            // This file contains the version number but we'll just hash it
            internal const string VersionHashFile = "assets/bin/Data/globalgamemanagers";

            internal const string MainAssetsFile1 = "assets/bin/Data/sharedassets17.assets";
            internal const string MainAssetsFile2 = "assets/bin/Data/sharedassets18.assets";
            internal const string RootPackFile1 = "assets/bin/Data/sharedassets19.assets";
            internal const string ColorsFile1 = "assets/bin/Data/sharedassets1.assets";
            internal const string TextFile1 = "assets/bin/Data/231368cb9c1d5dd43988f2a85226e7d7";
        }


        private ZipFile archive;

        public Version version;

        public enum Version {
            V1_0_0 = 0,
            V1_0_1,
            V1_0_2,
            V1_1_0,
        }

        public Apk(string path) {
            archive = ZipFile.Read(path);
            archive.CompressionLevel = CompressionLevel.None;
            version = DetectVersion();
        }

        public void Dispose() {
            archive.Save();
            archive.Dispose();
        }

        private string VersionFileHash() {
            SHA1 sha = SHA1Managed.Create();
            byte[] hash = sha.ComputeHash(ReadEntireEntry(FileConstants.VersionHashFile));
            return Convert.ToBase64String(hash);
        }

        private Version DetectVersion() {
            string hash = VersionFileHash();
            switch(hash) {
                case "wnEQdoDvVbzx+ZLyza/7M3dpgPA=":
                    return Version.V1_0_0;
                case "gdRgBAUWQhEstDEIkDMhIhsUpfw=":
                    return Version.V1_0_1;
                case "19H8crwCXHxdE8iX2mxH/Aeun1A=":
                    return Version.V1_0_2;
                case "NNVcU90Hhx0/iiX9zLP4qZ8MLdY=":
                    return Version.V1_1_0;
                default:
                    throw new ApplicationException($"Unrecognized APK version, hash {hash}");
            }
        }

        public string MainAssetsFile() {
            if(version >= Version.V1_1_0) {
                return FileConstants.MainAssetsFile2;
            } else {
                return FileConstants.MainAssetsFile1;
            }
        }

        public string RootPackFile()
        {
            return FileConstants.RootPackFile1;
        }

        public string ColorsFile()
        {
            return FileConstants.ColorsFile1;
        }

        public string TextFile()
        {
            return FileConstants.TextFile1;
        }

        public byte[] ReadEntireEntry(string entryPath) {
            ZipEntry entry = archive[entryPath];
            if(entry == null) return JoinedContents(entryPath);
            using (MemoryStream stream = new MemoryStream()) {
                entry.Extract(stream);
                stream.Close();
                return stream.ToArray();
            }
        }

        public void WriteEntireEntry(string entryPath, byte[] contents) {
            archive.UpdateEntry(entryPath,
                _name => new MemoryStream(contents),
                (_name, stream) => stream.Dispose());
        }

        public void ReplaceAssetsFile(string entryPath, byte[] contents) {
            DeleteSplits(entryPath);
            WriteEntireEntry(entryPath, contents);
        }

        public void CopyFileInto(string sourceFilePath, string destEntryPath) {
            archive.AddEntry(destEntryPath,
                _name => File.Open(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                (_name, stream) => stream.Close());
        }

        /// <summary>
        /// Deletes the given file from the APK
        /// </summary>
        /// <param name="filePath">The file to delete in the APK</param>
        public void RemoveFileAt(string filePath)
        {
            ZipEntry entry = archive[filePath];
            if (entry != null) archive.RemoveEntry(entry);
        }

        public byte[] JoinedContents(string basePath) {
            using (MemoryStream stream = new MemoryStream()) {
                string splitBase = basePath + ".split";
                for(int i = 0; ; i++) {
                    ZipEntry entry = archive[splitBase+i];
                    if(entry == null && i == 0) return null;
                    if(entry == null) break;
                    entry.Extract(stream);
                }
                stream.Close();
                return stream.ToArray();
            }
        }

        public void DeleteSplits(string basePath) {
            string splitBase = basePath + ".split";
            for(int i = 0; ; i++) {
                ZipEntry entry = archive[splitBase+i];
                if(entry == null) break;
                archive.RemoveEntry(entry);
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

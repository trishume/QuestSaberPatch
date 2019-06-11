using LibSaberPatch.AssetDataObjects;
using LibSaberPatch.BehaviorDataObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibSaberPatch
{
    public class AssetPtr
    {
        public int fileID;
        public ulong pathID;

        public AssetPtr(int _fileID, ulong _pathID) {
            fileID = _fileID;
            pathID = _pathID;
        }

        public AssetPtr(BinaryReader reader) {
            fileID = reader.ReadInt32();
            pathID = reader.ReadUInt64();
        }

        public AssetData Follow(SerializedAssets assets)
        {
            return assets.objects.FindLast(ao => ao.pathID == pathID).data;
        }

        public T Follow<T>(SerializedAssets assets) where T : AssetData
        {
            return (T)Follow(assets);
        }

        public T FollowToScript<T>(SerializedAssets assets) where T : BehaviorData
        {
            return (T)Follow<MonoBehaviorAssetData>(assets).data;
        }

        public void WriteTo(BinaryWriter w) {
            w.Write(fileID);
            w.Write(pathID);
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj.GetType().Equals(this))
            {
                var o = obj as AssetPtr;
                return fileID == o.fileID && pathID == o.pathID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (int)pathID + short.MaxValue * fileID;
        }
    }

    public abstract class AssetData
    {
        public abstract void WriteTo(BinaryWriter w, Apk.Version v);
        // Could also maybe make this method an actual method, instead of abstract, and use reflection.
        /// <summary>
        /// Traces all AssetPtrs owned by this AssetData and calls the action on all of them.
        /// </summary>
        /// <param name="action">The action to run on each AssetPtr.</param>
        public virtual void Trace(Action<AssetPtr> action)
        {
            // Defaults to nothing.
        }
        /// <summary>
        /// Returns a list of all owned files of this AssetData, it also checks its AssetPtrs.
        /// </summary>
        /// <param name="action">Returns a list of all owned files.</param>
        public virtual List<string> OwnedFiles(SerializedAssets assets)
        {
            // Default to return no owned files
            return new List<string>();
        }

        public virtual bool isScript<T>() {
            return false;
        }
    }

    public class AssetVector3
    {
        public float x;
        public float y;
        public float z;

        public AssetVector3(BinaryReader reader, int _length)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
        }

        public void WriteTo(BinaryWriter w)
        {
            w.Write(x);
            w.Write(y);
            w.Write(z);
        }
    }

    public class AssetVector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public AssetVector4(BinaryReader reader, int _length)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            w = reader.ReadSingle();
        }

        public void WriteTo(BinaryWriter w)
        {
            w.Write(x);
            w.Write(y);
            w.Write(z);
            w.Write(this.w);
        }
    }
}

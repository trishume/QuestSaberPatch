using System;
using System.IO;
using System.Collections.Generic;

namespace LibSaberPatch
{
    public abstract class BehaviorData
    {
        public abstract void WriteTo(BinaryWriter w, Apk.Version v);
        // Could maybe also make this method non-abstract using reflection
        /// <summary>
        /// Traces all AssetPtrs owned by this BehaviorData and calls the action on all of them.
        /// </summary>
        /// <param name="action">The action to run on each AssetPtr.</param>
        public virtual void Trace(Action<AssetPtr> action)
        {
            // Default to trace nothing
        }
        /// <summary>
        /// Returns a list of all owned files of this BehaviorData, it also checks its AssetPtrs.
        /// </summary>
        /// <param name="action">Returns a list of all owned files.</param>
        public virtual List<string> OwnedFiles(SerializedAssets assets)
        {
            // Default to return no owned files
            return new List<string>();
        }
    }
}

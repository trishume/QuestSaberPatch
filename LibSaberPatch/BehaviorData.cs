using System.IO;

namespace LibSaberPatch
{
    public abstract class BehaviorData
    {
        public abstract void WriteTo(BinaryWriter w);
    }

    public class UnknownBehaviorData : BehaviorData
    {
        public byte[] bytes;
        public UnknownBehaviorData(BinaryReader reader, int length) {
            bytes = reader.ReadBytes(length);
        }

        public override void WriteTo(BinaryWriter w) {
            w.Write(bytes);
        }
    }
}

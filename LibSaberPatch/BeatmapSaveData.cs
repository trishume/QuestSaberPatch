using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LibSaberPatch
{
    [Serializable]
    public class BeatmapSaveData
    {
        // Credit to https://stackoverflow.com/questions/5170333/binaryformatter-deserialize-unable-to-find-assembly-after-ilmerge
        const string AssemblyCSharp = "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        sealed class PreDeserializationBinder : SerializationBinder {
            public override Type BindToType(string assemblyName, string typeName) {
                // Console.WriteLine((assemblyName, typeName));
                Type typeToDeserialize = null;

                // For each assemblyName/typeName that you want to deserialize to
                // a different type, set typeToDeserialize to the desired type.
                String exeAssembly = Assembly.GetExecutingAssembly().FullName;


                // The following line of code returns the type.
                string newTypeName1 = typeName.Replace(AssemblyCSharp, exeAssembly);
                string newTypeName = Regex.Replace(newTypeName1, "(BeatmapSaveData|BeatmapEventType|NoteLineLayer|NoteType|NoteCutDirection|ObstacleType)", "LibSaberPatch.$1");
                string newAssembly = assemblyName.Replace(AssemblyCSharp, exeAssembly);
                // Console.WriteLine(("<to>",newAssembly, newTypeName));
                string typeDesc = String.Format("{0}, {1}", newTypeName, newAssembly);
                typeToDeserialize = Type.GetType(typeDesc);

                // Console.WriteLine(("<res>",typeDesc, typeToDeserialize));

                return typeToDeserialize;
            }
        }
        sealed class PreSerializationBinder : SerializationBinder {
            public override void BindToName(
                Type serializedType,
                out string assemblyName,
                out string typeName
            ) {
                // Console.WriteLine((serializedType.Name, serializedType.Assembly.FullName));
                Assembly thisAssembly = Assembly.GetExecutingAssembly();
                String exeAssembly = thisAssembly.FullName;
                if(serializedType.Assembly == thisAssembly) {
                    assemblyName = AssemblyCSharp;
                } else {
                    assemblyName = serializedType.Assembly.FullName;
                }
                string fullName = serializedType.FullName;
                typeName = fullName.Replace("LibSaberPatch.","").Replace(exeAssembly, AssemblyCSharp);
                // Console.WriteLine((typeName, assemblyName));
            }
            // This method is never called on serialization but we need to implement it to not be abstract
            public override Type BindToType(string assemblyName, string typeName) {
                // Console.WriteLine((assemblyName, typeName));
                return null;
            }
        }

        public virtual byte[] SerializeToBinary(bool deflate = true) {
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream()) {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Binder = new PreSerializationBinder();
                if(deflate) {
                    using (DeflateStream ds = new DeflateStream(memoryStream, CompressionMode.Compress)) {
                        bf.Serialize(ds, this);
                        ds.Flush();
                    }
                } else {
                    bf.Serialize(memoryStream, this);
                }
                memoryStream.Close();
                result = memoryStream.ToArray();
            }
            return result;
        }

        public static BeatmapSaveData DeserializeFromBinary(byte[] data, bool deflate = true) {
            BeatmapSaveData result;
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Binder = new PreDeserializationBinder();
                if(deflate) {
                    using (DeflateStream ds = new DeflateStream(memoryStream, CompressionMode.Decompress)) {
                        result = (BeatmapSaveData)bf.Deserialize(ds);
                    }
                } else {
                    result = (BeatmapSaveData)bf.Deserialize(memoryStream);
                }
            }
            return result;
        }

        public const string CurrentVersion = "2.0.0";

        public string _version;
        public List<BeatmapSaveData.EventData> _events;
        public List<BeatmapSaveData.NoteData> _notes;
        public List<BeatmapSaveData.ObstacleData> _obstacles;

        [Serializable]
        public class EventData {
            public float _time;
            public BeatmapEventType _type;
            public int _value;
        }

        [Serializable]
        public class NoteData {
            public float _time;
            public int _lineIndex;
            public NoteLineLayer _lineLayer;
            public NoteType _type;
            public NoteCutDirection _cutDirection;
        }

        [Serializable]
        public class ObstacleData {
            public float _time;
            public int _lineIndex;
            public ObstacleType _type;
            public float _duration;
            public int _width;
        }
    }

    public enum BeatmapEventType
    {
        Event0,
        Event1,
        Event2,
        Event3,
        Event4,
        Event5,
        Event6,
        Event7,
        Event8,
        Event9,
        Event10,
        Event11,
        Event12,
        Event13,
        Event14,
        Event15,
        VoidEvent = -1
    }

    public enum NoteLineLayer {
        Base,
        Upper,
        Top
    }

    public enum NoteType {
        NoteA,
        NoteB,
        GhostNote,
        Bomb
    }

    public enum NoteCutDirection {
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
        Any,
        None
    }

    public enum ObstacleType {
        FullHeight,
        Top
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Warp
{
    public struct YamlChunk
    {
        public int objectClassType;
        public string fileID;
        public string document;
    }

    public struct Tree<T>
    {
        public T element;
        public Tree<T>[] children;
    }

    public interface ITag
    {
        string TagName { get; }
        Dictionary<string, string> Attributes { get; }
    }

    public struct GameObjectElement
    {
        public Dictionary<string, ValueOrReference<object>> attributes;
        public IList<ValueOrReference<ComponentElement>> components;
    }

    public struct GameObjectElementRef
    {
        public Dictionary<string, ValueOrReference<object>> attributes;
        public IList<Reference> components;
    }

    public struct ComponentElement
    {
        public Dictionary<string, ValueOrReference<object>> attributes;
    }

    public struct ValueOrReference<T>
    {
        public T value;
        public Reference reference;
    }

    public struct Reference
    {
        public string fileID;
    }

    public struct Referenceable<T>
    {
        public string fileID;
        public T value;
    }

    public static class Encoder
    {
        public static void Encode(string prefabFilePath)
        {
            string text = System.IO.File.ReadAllText(prefabFilePath);
            var documents = SplitPrefabDocument(text);
            Debug.Log(string.Join("=======\n", documents));

            var objects = new List<Referenceable<object>>();

            foreach (var chunk in documents)
            {
                if (chunk.objectClassType == 1)
                {
                    objects.Add(ParseGameObjectDocument(chunk));
                }
            }
        }

        static ValueOrReference<object> ParseAttribute(object attr)
        {
            if (attr is Dictionary<string, object>)
            {
                var obj = attr as Dictionary<string, object>;

                if (obj.ContainsKey("fileID"))
                {
                    return new ValueOrReference<object>
                    {
                        reference = new Reference
                        {
                            fileID = obj["fileID"] as string
                        }
                    };
                }
            }

            return new ValueOrReference<object>
            {
                value = attr
            };
        }

        static Referenceable<object> ParseGameObjectDocument(YamlChunk chunk)
        {
            // component: {fileID: 8345145045156654008}
            var deserializer = new DeserializerBuilder().Build();
            var parsed = deserializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(chunk.document);
            var attributes = parsed.Values.First();
            var components = attributes["m_Component"] as IList<object>;
            attributes.Remove("m_Component");

            return new Referenceable<object>
            {
                fileID = chunk.fileID,
                value = new GameObjectElement
                {
                    attributes = attributes
                    .ToDictionary(
                        (entry) => entry.Key,
                                (entry) => ParseAttribute(entry.Value)),
                    components = components.Select(comp =>
                    {
                        var c = comp as IDictionary<object, object>;
                        var value = c["component"] as Dictionary<object, object>;
                        return new ValueOrReference<ComponentElement>
                        {
                            reference = new Reference
                            {
                                fileID = value["fileID"] as string
                            }
                        };
                    })
                    .ToList()
                }
            };
        }

        static IList<YamlChunk> SplitPrefabDocument(string prefabText)
        {
            var commentRegex = new Regex(@"^%.+\n", RegexOptions.Multiline);
            var objStartRegex = new Regex(@"^ !u!([0-9]+) &([0-9]+)\n");

            return commentRegex.Replace(prefabText, string.Empty)
                .Split(new string[] { "---" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(chunk =>
                {
                    var document = objStartRegex.Replace(chunk, string.Empty);
                    var match = objStartRegex.Match(chunk);

                    return new YamlChunk
                    {
                        objectClassType = int.Parse(match.Groups[1].Value),
                        fileID = match.Groups[2].Value,
                        document = document
                    };
                })
                .ToList();
        }
    }
}

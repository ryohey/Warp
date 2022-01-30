using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
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

    public interface IElementRef { }

    public struct GameObjectElementRef : IElementRef
    {
        public IDictionary<string, ValueOrReference<object>> attributes;
        public IList<Reference> components;
    }

    public struct TransformElementRef : IElementRef
    {
        public IDictionary<string, ValueOrReference<object>> attributes;
        public IList<Reference> children;
    }

    public struct ComponentElementRef : IElementRef
    {
        public IDictionary<string, ValueOrReference<object>> attributes;
    }

    public interface IElement { }

    [Serializable]
    public struct GameObjectElement : IElement
    {
        public Dictionary<string, object> attributes;
        public List<ComponentElement> components;
        public List<GameObjectElement> children;
    }

    [Serializable]
    public struct ComponentElement : IElement
    {
        public Dictionary<string, object> attributes;
    }

    public struct ValueOrReference<T>
    {
        public T value;
        public Reference? reference;
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

            var objects = new List<Referenceable<IElementRef>>();

            foreach (var chunk in documents)
            {
                if (chunk.objectClassType == 1)
                {
                    objects.Add(ParseGameObjectDocument(chunk));
                }
                else if (chunk.objectClassType == 4)
                {
                    objects.Add(ParseTransformDocument(chunk));
                }
                else
                {
                    objects.Add(ParseComponentDocument(chunk));
                }
            }

            var gameObjectElement = ToGameObjectElement(objects);
            string json = JsonConvert.SerializeObject(gameObjectElement);

            Debug.Log(json);
        }

        static GameObjectElement ToGameObjectElement(List<Referenceable<IElementRef>> objects)
        {
            IElementRef ResolveReference(Reference reff)
            {
                if (reff.fileID == "0")
                {
                    return null;
                }
                var obj = objects.Where(obj => obj.fileID == reff.fileID)
                    .Cast<Referenceable<IElementRef>?>()
                    .FirstOrDefault();
                if (obj.HasValue)
                {
                    return obj.Value.value;
                }
                throw new Exception("Cannot resolve reference for fileID: " + reff.fileID);
            }

            Dictionary<string, object> ResolveAttributes(IDictionary<string, ValueOrReference<object>> attributes)
            {
                return attributes.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.reference.HasValue ? ResolveReference(entry.Value.reference.Value) : entry.Value.value
                    );
            }

            GameObjectElement CreateGameObjectElement(TransformElementRef transform)
            {
                var gameObject = objects
                    .Where(obj => obj.value is GameObjectElementRef && obj.fileID == transform.attributes["m_GameObject"].reference.Value.fileID)
                    .Cast<Referenceable<IElementRef>?>()
                    .FirstOrDefault();

                if (!gameObject.HasValue)
                {
                    throw new Exception("GameObject not found");
                }

                var go = (GameObjectElementRef)(gameObject.Value.value);

                return new GameObjectElement
                {
                    attributes = ResolveAttributes(go.attributes),
                    children = transform.children.Select(t =>
                    {
                        var elm = (TransformElementRef)ResolveReference(t);
                        return new GameObjectElement
                        {
                            attributes = ResolveAttributes(elm.attributes),
                            children = elm.children.Select(ResolveReference).Cast<GameObjectElement>().ToList()
                        };
                    }).ToList()
                };
            }

            var rootTransform = objects
                .Where(obj =>
            {
                if (obj.value is TransformElementRef elm)
                {
                    if (elm.attributes["m_Father"].reference.Value.fileID == "0")
                    {
                        return true;
                    }
                }
                return false;
            })
                .Cast<Referenceable<IElementRef>?>()
                .FirstOrDefault();

            if (!rootTransform.HasValue)
            {
                throw new Exception("Root Transform not found");
            }

            return CreateGameObjectElement((TransformElementRef)(rootTransform.Value.value));
        }

        static ValueOrReference<object> ParseAttribute(object attr)
        {
            if (attr is Dictionary<object, object>)
            {
                var obj = attr as Dictionary<object, object>;

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

        static Referenceable<IElementRef> ParseGameObjectDocument(YamlChunk chunk)
        {
            var deserializer = new DeserializerBuilder().Build();
            var parsed = deserializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(chunk.document);
            var attributes = parsed.Values.First();
            var components = attributes["m_Component"] as IList<object>;
            attributes.Remove("m_Component");

            return new Referenceable<IElementRef>
            {
                fileID = chunk.fileID,
                value = new GameObjectElementRef
                {
                    attributes = attributes
                    .ToDictionary(
                        (entry) => entry.Key,
                                (entry) => ParseAttribute(entry.Value)),
                    components = components.Select(comp =>
                    {
                        // component: {fileID: 8345145045156654008}
                        var c = comp as IDictionary<object, object>;
                        var value = c["component"] as Dictionary<object, object>;
                        return new Reference
                        {
                            fileID = value["fileID"] as string
                        };
                    })
                    .ToList()
                }
            };
        }

        static Referenceable<IElementRef> ParseTransformDocument(YamlChunk chunk)
        {
            var deserializer = new DeserializerBuilder().Build();
            var parsed = deserializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(chunk.document);
            var attributes = parsed.Values.First();
            var children = attributes["m_Children"] as IList<object>;
            attributes.Remove("m_Children");

            return new Referenceable<IElementRef>
            {
                fileID = chunk.fileID,
                value = new TransformElementRef
                {
                    attributes = attributes
                    .ToDictionary(
                        (entry) => entry.Key,
                                (entry) => ParseAttribute(entry.Value)),
                    children = children.Select(comp =>
                    {
                        var c = comp as IDictionary<object, object>;
                        return new Reference
                        {
                            fileID = c["fileID"] as string
                        };
                    })
                    .ToList()
                }
            };
        }

        static Referenceable<IElementRef> ParseComponentDocument(YamlChunk chunk)
        {
            var deserializer = new DeserializerBuilder().Build();
            var parsed = deserializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(chunk.document);
            var attributes = parsed.Values.First();

            return new Referenceable<IElementRef>
            {
                fileID = chunk.fileID,
                value = new ComponentElementRef
                {
                    attributes = attributes
                    .ToDictionary(
                        (entry) => entry.Key,
                                (entry) => ParseAttribute(entry.Value)),
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

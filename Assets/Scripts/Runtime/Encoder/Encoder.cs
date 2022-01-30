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
        public IDictionary<string, object> attributes;
    }

    public struct Tree<T>
    {
        public T element;
        public Tree<T>[] children;
    }

    public interface ITag
    {
        string TagName { get; }
        IDictionary<string, string> Attributes { get; }
    }

    public interface IElement { }

    public struct GameObjectElement : IElement
    {
        public IDictionary<string, object> attributes;
        public IList<ComponentElement> components;
        public IList<GameObjectElement> children;
    }

    public struct ComponentElement : IElement
    {
        public IDictionary<string, object> attributes;
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
        static IList<YamlChunk> SplitPrefabDocument(string prefabText)
        {
            var deserializer = new DeserializerBuilder().Build();
            var commentRegex = new Regex(@"^%.+\n", RegexOptions.Multiline);
            var objStartRegex = new Regex(@"^ !u!([0-9]+) &([0-9]+)\n");

            return commentRegex.Replace(prefabText, string.Empty)
                .Split(new string[] { "---" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(chunk =>
                {
                    var document = objStartRegex.Replace(chunk, string.Empty);
                    var match = objStartRegex.Match(chunk);
                    var parsed = deserializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(document);
                    var attributes = parsed.Values.First();

                    return new YamlChunk
                    {
                        objectClassType = int.Parse(match.Groups[1].Value),
                        fileID = match.Groups[2].Value,
                        attributes = attributes
                    };
                })
                .ToList();
        }

        public static T GetValueAsDictionary<T>(this object self, string key)
        {
            var dict = self as IDictionary<object, object>;
            if (dict == null)
            {
                throw new Exception("object is not dictionary");
            }
            var value = dict[key];
            if (value is T v)
            {
                return v;
            }
            throw new Exception($"{key} {value.GetType().Name} is not {typeof(T).Name}");
        }

        public static void Encode(string prefabFilePath)
        {
            string text = System.IO.File.ReadAllText(prefabFilePath);
            var documents = SplitPrefabDocument(text);

            ComponentElement CreateComponentElement(string fileID)
            {
                var chunk = documents.First(obj => obj.fileID == fileID);

                return new ComponentElement
                {
                    attributes = chunk.attributes
                };
            }

            GameObjectElement CreateGameObjectElement(YamlChunk transform)
            {
                var gameObject = documents
                    .First(obj => obj.fileID == transform.attributes["m_GameObject"].GetValueAsDictionary<string>("fileID"));

                // List of {fileID: 8345145045156654008}
                var children = transform.attributes["m_Children"] as IList<object>;

                var attributes = new Dictionary<string, object>(gameObject.attributes);

                // List of {component: {fileID: 8345145045156654008}}
                var components = attributes["m_Component"] as IList<object>;
                attributes.Remove("m_Component");

                return new GameObjectElement
                {
                    attributes = attributes,
                    children = children.Select(t =>
                    {
                        var fileID = t.GetValueAsDictionary<string>("fileID");
                        return CreateGameObjectElement(documents.First(chunk => chunk.fileID == fileID));
                    }).ToList(),
                    components = components.Select(t =>
                    {
                        var fileID = t
                            .GetValueAsDictionary<object>("component")
                            .GetValueAsDictionary<string>("fileID");
                        return CreateComponentElement(fileID);
                    }).ToList()
                };
            }

            var rootTransform = documents
                .Where(chunk => chunk.objectClassType == 4 && chunk.attributes["m_Father"]?.GetValueAsDictionary<string>("fileID") == "0")
                .First();

            var gameObjectElement = CreateGameObjectElement(rootTransform);
            string json = JsonConvert.SerializeObject(gameObjectElement);

            Debug.Log(json);
        }
    }
}

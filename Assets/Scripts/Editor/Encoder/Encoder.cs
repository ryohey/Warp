using System;
using System.Collections.Generic;
using System.IO;
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
        // https://docs.unity3d.com/ja/2020.3/Manual/ClassIDReference.html
        public int classID;
        public string fileID;
        public string typeName;
        public IDictionary<string, object> properties;
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
                    var properties = parsed.Values.First();

                    return new YamlChunk
                    {
                        classID = int.Parse(match.Groups[1].Value),
                        fileID = match.Groups[2].Value,
                        typeName = parsed.Keys.First(),
                        properties = properties
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
                var classType = TypeUtils.GetUnityType(chunk.typeName);
                var properties = FixProperties(chunk.properties, classType);

                // Associations between Components and GameObject is represented by tree structure,
                // so we don't need these references to GameObject
                properties.Remove("m_GameObject");

                if (chunk.classID == 4)
                {
                    properties.Remove("m_Father");
                    properties.Remove("m_Children");
                }

                return new ComponentElement
                {
                    typeName = chunk.typeName,
                    classID = chunk.classID,
                    fileID = chunk.fileID,
                    properties = properties
                };
            }

            GameObjectElement CreateGameObjectElement(YamlChunk transform)
            {
                var gameObject = documents
                    .First(obj => obj.fileID == transform.properties["m_GameObject"].GetValueAsDictionary<string>("fileID"));

                // List of {fileID: 8345145045156654008}
                var children = transform.properties["m_Children"] as IList<object>;

                var properties = new Dictionary<string, object>(gameObject.properties);

                // List of {component: {fileID: 8345145045156654008}}
                var components = properties["m_Component"] as IList<object>;
                properties.Remove("m_Component");

                var classType = TypeUtils.GetUnityType(gameObject.typeName);

                return new GameObjectElement
                {
                    typeName = gameObject.typeName,
                    fileID = gameObject.fileID,
                    properties = FixProperties(properties, classType),
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
                .Where(chunk => chunk.classID == 4 && chunk.properties["m_Father"]?.GetValueAsDictionary<string>("fileID") == "0")
                .First();

            var gameObjectElement = CreateGameObjectElement(rootTransform);
            string json = JsonConvert.SerializeObject(gameObjectElement, Formatting.Indented);

            Debug.Log(json);

            Debug.Log("Create AssetBundles...");

            AssetResolver.CreateDependantAssetBundles(gameObjectElement);

            var jsonPath = prefabFilePath + ".json";
            File.WriteAllText(jsonPath, json);

            Debug.Log($"Save Prefab {prefabFilePath} to {jsonPath}");
        }

        private static IDictionary<string, object> FixProperties(IDictionary<string, object> dict, Type classType)
        {
            return dict.ToDictionary(
                entry => entry.Key,
                entry =>
                {
                    if (IsFileIDZero(entry.Value))
                    {
                        return null;
                    }

                    return entry.Value;
                }
            );
        }

        private static object ValueToNumber(object value)
        {
            if (value is string str)
            {
                if (str.Contains("."))
                {
                    double.TryParse(str, out var outVal);

                    if (double.IsNaN(outVal) || double.IsInfinity(outVal))
                    {
                        return 0;
                    }
                    return outVal;
                }
                else
                {
                    int.TryParse(str, out var outVal);
                    return outVal;
                }
            }

            return value;
        }

        private static bool IsFileIDZero(object value)
        {
            var d = value as IDictionary<object, object>;
            return d != null && d.ContainsKey("fileID") && d["fileID"] as string == "0";
        }
    }
}

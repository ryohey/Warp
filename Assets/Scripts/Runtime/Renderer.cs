using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Warp
{
    public class Renderer
    {
        public static void SpawnPrefab(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var gameObjectElement = JsonConvert.DeserializeObject<GameObjectElement>(json);

            // fileID to Object mapping
            var objectMap = new Dictionary<string, UnityEngine.Object>();
            Spawn(gameObjectElement, null, objectMap);
            Update(gameObjectElement, objectMap);
        }

        public static void Spawn(GameObjectElement element, Transform parent, IDictionary<string, UnityEngine.Object> objectMap)
        {
            var gameObject = new GameObject();
            objectMap[element.fileID] = gameObject;

            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }

            foreach (var comp in element.components)
            {
                var type = GetUnityType(comp.typeName);
                if (type == null)
                {
                    throw new Exception($"Failed to get type: {comp.typeName}");
                }
                var instance = type == typeof(Transform) ?
                    gameObject.transform : gameObject.AddComponent(type);
                if (instance == null)
                {
                    throw new Exception($"Failed to create instance: {comp.typeName}");
                }
                objectMap[comp.fileID] = instance;
            }

            foreach (var child in element.children)
            {
                Spawn(child, gameObject.transform, objectMap);
            }
        }

        public static void Update(GameObjectElement element, IDictionary<string, UnityEngine.Object> objectMap)
        {
            var gameObject = objectMap[element.fileID];
            UpdateProperties(gameObject, element.properties);

            foreach (var comp in element.components)
            {
                var instance = objectMap[comp.fileID];
                UpdateProperties(instance, comp.properties);
            }

            foreach (var childElm in element.children)
            {
                Update(childElm, objectMap);
            }
        }

        public static Type GetUnityType(string className)
        {
            return Type.GetType($"UnityEngine.{className}, UnityEngine.dll");
        }

        private static void UpdateProperties(object target, IDictionary<string, object> properties)
        {
            foreach (var entry in properties)
            {
                var type = target.GetType();
                var propName = Regex.Replace(entry.Key, @"^m_", string.Empty)
                    .FirstCharToLowerCase();
                var prop = type.GetProperty(propName);

                //var field = type.GetField(propName);
                //if (field != null)
                //{
                //    field.SetValue(gameObject, entry.Value);
                //}

                if (prop != null)
                {
                    if (entry.Value is string value)
                    {
                        if (prop.PropertyType == typeof(string))
                        {
                            prop.SetValue(target, value);
                        }
                        else if (prop.PropertyType == typeof(bool))
                        {
                            if (int.TryParse(value, out var outVal))
                            {
                                prop.SetValue(target, outVal == 1);
                            }
                        }
                        else if (prop.PropertyType == typeof(int))
                        {
                            if (int.TryParse(value, out var outVal))
                            {
                                prop.SetValue(target, outVal);
                            }
                        }
                        else if (prop.PropertyType == typeof(long))
                        {
                            if (long.TryParse(value, out var outVal))
                            {
                                prop.SetValue(target, outVal);
                            }
                        }
                        else if (prop.PropertyType == typeof(float))
                        {
                            if (float.TryParse(value, out var outVal))
                            {
                                prop.SetValue(target, outVal);
                            }
                        }
                        else if (prop.PropertyType == typeof(double))
                        {
                            if (double.TryParse(value, out var outVal))
                            {
                                prop.SetValue(target, outVal);
                            }
                        }
                    }
                    else if (entry.Value is JObject obj)
                    {
                        if (prop.PropertyType == typeof(Vector2Int))
                        {
                            var x = obj["x"].Value<string>();
                            var y = obj["y"].Value<string>();
                            if (int.TryParse(x, out var outX)
                                && int.TryParse(y, out var outY))
                            {
                                prop.SetValue(target, new Vector2Int(outX, outY));
                            }
                        }
                        else if (prop.PropertyType == typeof(Vector2))
                        {
                            var x = obj["x"].Value<string>();
                            var y = obj["y"].Value<string>();
                            if (float.TryParse(x, out var outX)
                                && float.TryParse(y, out var outY))
                            {
                                prop.SetValue(target, new Vector2(outX, outY));
                            }
                        }
                        else if (prop.PropertyType == typeof(Vector3Int))
                        {
                            var x = obj["x"].Value<string>();
                            var y = obj["y"].Value<string>();
                            var z = obj["z"].Value<string>();
                            if (int.TryParse(x, out var outX)
                                && int.TryParse(y, out var outY)
                                && int.TryParse(z, out var outZ))
                            {
                                prop.SetValue(target, new Vector3Int(outX, outY, outZ));
                            }
                        }
                        else if (prop.PropertyType == typeof(Vector3))
                        {
                            var x = obj["x"].Value<string>();
                            var y = obj["y"].Value<string>();
                            var z = obj["z"].Value<string>();
                            if (float.TryParse(x, out var outX)
                                && float.TryParse(y, out var outY)
                                && float.TryParse(z, out var outZ))
                            {
                                prop.SetValue(target, new Vector3(outX, outY, outZ));
                            }
                        }
                        else if (prop.PropertyType == typeof(Vector4))
                        {
                            var x = obj["x"].Value<string>();
                            var y = obj["y"].Value<string>();
                            var z = obj["z"].Value<string>();
                            var w = obj["w"].Value<string>();
                            if (float.TryParse(x, out var outX)
                                && float.TryParse(y, out var outY)
                                && float.TryParse(z, out var outZ)
                                && float.TryParse(w, out var outW))
                            {
                                prop.SetValue(target, new Vector4(outX, outY, outZ, outW));
                            }
                        }
                        else if (prop.PropertyType == typeof(Quaternion))
                        {
                            var x = obj["x"].Value<string>();
                            var y = obj["y"].Value<string>();
                            var z = obj["z"].Value<string>();
                            var w = obj["w"].Value<string>();
                            if (float.TryParse(x, out var outX)
                                && float.TryParse(y, out var outY)
                                && float.TryParse(z, out var outZ)
                                && float.TryParse(w, out var outW))
                            {
                                prop.SetValue(target, new Quaternion(outX, outY, outZ, outW));
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"Property {propName} is not found in the type {type.Name}");
                }
            }
        }
    }

    public static class StringExtensions
    {
        public static string FirstCharToLowerCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;

            return char.ToLower(str[0]) + str.Substring(1);
        }
    }
}


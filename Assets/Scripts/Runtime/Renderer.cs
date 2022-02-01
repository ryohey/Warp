using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace Warp
{
    public class Renderer
    {
        public static void SpawnPrefab(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var gameObjectElement = JsonConvert.DeserializeObject<GameObjectElement>(json);
            Spawn(gameObjectElement, null);
        }

        public static void Spawn(GameObjectElement element, Transform parent)
        {
            var gameObject = new GameObject();

            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }

            foreach (var entry in element.properties)
            {
                var type = gameObject.GetType();
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
                            prop.SetValue(gameObject, value);
                        }
                        else if (prop.PropertyType == typeof(int))
                        {
                            if (int.TryParse(value, out var outVal))
                            {
                                prop.SetValue(gameObject, outVal);
                            }
                        }
                        else if (prop.PropertyType == typeof(long))
                        {
                            if (long.TryParse(value, out var outVal))
                            {
                                prop.SetValue(gameObject, outVal);
                            }
                        }
                        else if (prop.PropertyType == typeof(double))
                        {
                            if (double.TryParse(value, out var outVal))
                            {
                                prop.SetValue(gameObject, outVal);
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"Property {propName} is not found in the type {type.Name}");
                }
            }

            foreach (var comp in element.components)
            {
                var type = GetUnityType(comp.typeName);
                var instance = gameObject.AddComponent(type);
                // TODO: update instance
            }

            foreach (var child in element.children)
            {
                Spawn(child, gameObject.transform);
            }
        }

        public static Type GetUnityType(string className)
        {
            return Type.GetType($"UnityEngine.{className}, UnityEngine.dll");
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


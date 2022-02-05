using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Warp
{
    public class Renderer
    {
        private readonly AssetLoader assetLoader;
        private AssetLoader assetLoader1;

        public Renderer(AssetLoader assetLoader)
        {
            this.assetLoader = assetLoader;
        }

        public RenderContext SpawnPrefab(string jsonPath, Transform parent = null)
        {
            var json = File.ReadAllText(jsonPath);
            var gameObjectElement = JsonConvert.DeserializeObject<GameObjectElement>(json);

            var context = new RenderContext();
            Spawn(gameObjectElement, parent, context);
            Update(gameObjectElement, context);

            return context;
        }

        public void UpdatePrefab(string jsonPath, RenderContext context)
        {
            var json = File.ReadAllText(jsonPath);
            var gameObjectElement = JsonConvert.DeserializeObject<GameObjectElement>(json);
            Update(gameObjectElement, context);
        }

        public FileSystemWatcher WatchPrefab(string jsonPath, Transform parent = null)
        {
            var mainThreadContext = SynchronizationContext.Current;
            var context = SpawnPrefab(jsonPath, parent);
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(jsonPath))
            {
                Filter = Path.GetFileName(jsonPath),
                EnableRaisingEvents = true,
            };
            watcher.Changed += (sender, ev) =>
            {
                Debug.Log($"{ev.ChangeType}: {jsonPath}");

                mainThreadContext.Post(__ =>
                {
                    try
                    {
                        UpdatePrefab(jsonPath, context);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }, null);
            };
            return watcher;
        }

        public void Spawn(GameObjectElement element, Transform parent, RenderContext context)
        {
            var gameObject = new GameObject();
            context.objectMap[element.fileID] = gameObject.GetInstanceID();

            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }

            foreach (var comp in element.components)
            {
                var type = TypeUtils.GetUnityType(comp.typeName);
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
                context.objectMap[comp.fileID] = instance.GetInstanceID();
            }

            foreach (var child in element.children)
            {
                Spawn(child, gameObject.transform, context);
            }
        }

        public void Update(GameObjectElement element, RenderContext context)
        {
            var gameObject = context.FindObject(element.fileID);
            UpdateProperties(gameObject, element.properties);

            foreach (var comp in element.components)
            {
                var instance = context.FindObject(comp.fileID);
                UpdateProperties(instance, comp.properties);
            }

            foreach (var childElm in element.children)
            {
                Update(childElm, context);
            }
        }

        private void UpdateProperties(object target, IDictionary<string, object> properties)
        {
            foreach (var entry in properties)
            {
                var type = target.GetType();
                var propName = TypeUtils.FixPropName(entry.Key);

                if (propName == "materials")
                {
                    Debug.Log("break");
                }

                var prop = type.GetProperty(propName);

                //var field = type.GetField(propName);
                //if (field != null)
                //{
                //    field.SetValue(gameObject, entry.Value);
                //}

                if (prop == null)
                {
                    Debug.Log($"Property {propName} is not found in the type {type.Name}");
                    continue;
                }

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
                    else if (prop.PropertyType == typeof(uint))
                    {
                        if (uint.TryParse(value, out var outVal))
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
                    else if (prop.PropertyType.IsEnum)
                    {
                        var enumType = prop.PropertyType.GetEnumUnderlyingType();
                        if (enumType == typeof(int))
                        {
                            if (int.TryParse(value, out var outVal))
                            {
                                prop.SetValue(target, Enum.ToObject(prop.PropertyType, outVal));
                            }
                            else
                            {
                                Debug.LogWarning($"Enum type {enumType.Name} does not match {value}");
                            }
                        }
                        else if (enumType == typeof(string))
                        {
                            prop.SetValue(target, Enum.Parse(prop.PropertyType, value));
                        }
                        else
                        {
                            Debug.LogWarning($"Unsupported enum type {enumType.Name}");
                        }
                    }
                    else
                    {
                        Debug.Log($"Not supported to parse {prop.PropertyType.Name} {prop.Name}");
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
                    else if (prop.PropertyType == typeof(Mesh))
                    {
                        var guid = obj["guid"].Value<string>();
                        var mesh = assetLoader.Load<Mesh>(guid);
                        prop.SetValue(target, mesh);
                    }
                    else if (prop.PropertyType == typeof(Color))
                    {
                        var r = obj["r"].Value<string>();
                        var g = obj["g"].Value<string>();
                        var b = obj["b"].Value<string>();
                        var a = obj["a"].Value<string>();
                        if (float.TryParse(r, out var outR)
                            && float.TryParse(g, out var outG)
                            && float.TryParse(b, out var outB)
                            && float.TryParse(a, out var outA))
                        {
                            prop.SetValue(target, new Color(outR, outG, outB, outA));
                        }
                    }
                    else
                    {
                        Debug.Log($"Not supported to parse {prop.PropertyType.Name} {prop.Name}");
                    }
                }
                else if (entry.Value is JArray array)
                {
                    if (prop.PropertyType.IsArray)
                    {
                        var elementType = prop.PropertyType.GetElementType();
                        if (elementType == typeof(Material))
                        {
                            var materials = new List<Material>();
                            foreach (var item in array)
                            {
                                var dict = item.Value<JObject>();
                                var guid = dict["guid"].Value<string>();
                                var material = assetLoader.Load<Material>(guid);
                                materials.Add(material);
                            }
                            prop.SetValue(target, materials.ToArray());
                        }
                        Debug.Log(prop.Name);
                    }
                    else
                    {
                        Debug.Log($"Cannot assign array to the non-array property {prop.Name}");
                    }
                }
            }
        }
    }
}


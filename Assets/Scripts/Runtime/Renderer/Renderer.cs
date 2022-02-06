using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly IAssetLoader assetLoader;

        public Renderer(IAssetLoader assetLoader)
        {
            this.assetLoader = assetLoader;
        }

        public GameObject Spawn(GameObjectElement element, Transform parent, RenderContext context)
        {
            var gameObject = new GameObject();
            context.objectMap[element.fileID] = gameObject.GetInstanceID();

            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }

            foreach (var comp in element.components)
            {
                SpawnComponent(gameObject, comp, context);
            }

            foreach (var child in element.children)
            {
                Spawn(child, gameObject.transform, context);
            }

            return gameObject;
        }

        private void SpawnComponent(GameObject gameObject, ComponentElement element, RenderContext context)
        {
            var type = TypeUtils.GetUnityType(element.typeName);
            if (type == null)
            {
                throw new Exception($"Failed to get type: {element.typeName}");
            }
            var instance = type == typeof(Transform) ?
                gameObject.transform : gameObject.AddComponent(type);
            if (instance == null)
            {
                throw new Exception($"Failed to create instance: {element.typeName}");
            }
            context.objectMap[element.fileID] = instance.GetInstanceID();
        }

        private void DestroyExcessObjects(IEnumerable<UnityEngine.Object> objects, IEnumerable<IElement> elements, RenderContext context)
        {
            var existingObjects = elements
                .Select(obj => context.FindObject(obj.FileID))
                .Where(obj => obj != null)
                .ToList();

            objects.Except(existingObjects)
                .ToList()
                .ForEach(obj =>
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                });
        }

        public void Reconstruct(GameObjectElement element, Transform parent, RenderContext context)
        {
            var gameObject = context.FindObject(element.fileID) as GameObject;

            if (gameObject == null)
            {
                Spawn(element, parent, context);
                return;
            }

            var children = gameObject.transform
                .Cast<Transform>()
                .Select(t => t.gameObject)
                .ToList();

            DestroyExcessObjects(
                children.Cast<UnityEngine.Object>(),
                element.children.Cast<IElement>(),
                context
            );

            var components = gameObject.GetComponents<Component>();

            DestroyExcessObjects(
                components.Cast<UnityEngine.Object>(),
                element.components.Cast<IElement>(),
                context
            );

            foreach (var comp in element.components)
            {
                if (context.FindObject(comp.fileID) != null)
                {
                    continue;
                }
                SpawnComponent(gameObject, comp, context);
            }

            foreach (var childElm in element.children)
            {
                Reconstruct(childElm, gameObject.transform, context);
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


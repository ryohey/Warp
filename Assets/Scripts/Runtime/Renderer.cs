using System;
using System.IO;
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

            // TODO; update gameObject

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
}


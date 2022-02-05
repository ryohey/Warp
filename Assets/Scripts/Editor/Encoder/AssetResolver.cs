using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Warp
{
    public static class AssetResolver
    {
        public static void CreateDependantAssetBundles(IElement element)
        {
            if (element is GameObjectElement gameObjectElement)
            {
                CreateAssetBundlesInProperties(gameObjectElement.properties);

                foreach (var component in gameObjectElement.components)
                {
                    CreateDependantAssetBundles(component);
                }

                foreach (var child in gameObjectElement.children)
                {
                    CreateDependantAssetBundles(child);
                }
            }
            else if (element is ComponentElement componentElement)
            {
                CreateAssetBundlesInProperties(componentElement.properties);
            }
        }

        private static void CreateAssetBundlesInProperties(IDictionary<string, object> properties)
        {
            foreach (var entry in properties)
            {
                var guid = TryGetGUID(entry.Value);
                if (guid != null)
                {
                    CreateAssetBundle(guid);
                }
                else if (entry.Value is IList<object> list)
                {
                    foreach (var item in list)
                    {
                        var itemGuid = TryGetGUID(item);
                        if (itemGuid != null)
                        {
                            CreateAssetBundle(itemGuid);
                        }
                    }
                }
            }
        }

        private static string TryGetGUID(object value)
        {
            if (value is IDictionary<object, object> dict
            && dict.ContainsKey("guid")
            && dict["guid"] is string guid)
            {
                return guid;
            }
            return null;
        }

        private static void CreateAssetBundle(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"ResolveMesh {guid} -> {path}");

            // Library/unity default resources
            if (path.Contains("unity default resources"))
            {
                Debug.LogWarning("Built-in assets are not supported");
                return;
            }

            var assetBundleBuild = new AssetBundleBuild
            {
                assetBundleName = guid,
                assetNames = new[] { path }
            };

            var manifest = BuildPipeline.BuildAssetBundles(
                $"AssetBundle",
                new[] { assetBundleBuild },
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneOSX
                );

            Debug.Log($"AssetBundle {guid} is created");
        }
    }
}

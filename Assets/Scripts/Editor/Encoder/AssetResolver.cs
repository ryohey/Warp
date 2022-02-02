using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Warp
{
    public static class AssetResolver
    {
        public static IDictionary<string, object> ResolveMesh(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"ResolveMesh {guid} -> {path}");

            // Library/unity default resources
            if (path.Contains("unity default resources"))
            {
                Debug.LogWarning("Built-in assets are not supported");
                return null;
            }

            return new Dictionary<string, object>
            {
                { "type", "Mesh" },
                { "path", path }
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Warp
{
    public class WarpEditor
    {
        public static readonly WarpEditor Instance = new WarpEditor();
        public List<string> willSavePrefabPaths = new List<string>();
        public bool isEnabled = false;

        public void OnPostprocessAllAssets()
        {
            if (!isEnabled)
            {
                return;
            }

            var paths = willSavePrefabPaths
                .Where(path => path.StartsWith("Assets/Prefabs/"));

            foreach (var path in paths)
            {
                var gameObjectElement = Encoder.Encode(path);
                string json = JsonConvert.SerializeObject(gameObjectElement, Formatting.Indented);
                var jsonPath = Path.Combine("static", Path.GetFileName(path) + ".json");
                File.WriteAllText(jsonPath, json);
                Debug.Log($"Save Prefab {path} to {jsonPath}");

                Debug.Log("Create AssetBundles...");
                var assetResolver = new AssetResolver("static/");
                assetResolver.CreateDependantAssetBundles(gameObjectElement);
            }
        }
    }
}

#if UNITY_EDITOR

using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Warp
{
    class WarpWindow : EditorWindow
    {
        private RenderContext context;
        private FileSystemWatcher watcher;
        private Renderer renderer = new Renderer(new AssetLoader("AssetBundle"));
        private Server server;

        [MenuItem("Warp/Edit")]
        public static void ShowWindow()
        {
            GetWindow(typeof(WarpWindow));
        }

        void OnGUI()
        {
            var baseDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "static");

            if (GUILayout.Button("Convert prefab"))
            {
                var prefabFilePath = @"Assets/Prefabs/GameObject.prefab";
                var gameObjectElement = Encoder.Encode(prefabFilePath);

                string json = JsonConvert.SerializeObject(gameObjectElement, Formatting.Indented);
                Debug.Log(json);
                var jsonPath = Path.Combine(baseDir, Path.GetFileName(prefabFilePath) + ".json");
                File.WriteAllText(jsonPath, json);
                Debug.Log($"Save Prefab {prefabFilePath} to {jsonPath}");

                Debug.Log("Create AssetBundles...");
                var assetResolver = new AssetResolver("static/");
                assetResolver.CreateDependantAssetBundles(gameObjectElement);
            }

            if (GUILayout.Button("Spawn prefab"))
            {
                context = renderer.SpawnPrefab(@"Assets/Prefabs/GameObject.prefab.json");
            }

            if (GUILayout.Button("Update prefab") && context != null)
            {
                renderer.UpdatePrefab(@"Assets/Prefabs/GameObject.prefab.json", context);
            }

            if (GUILayout.Button("Spawn and Synchronize"))
            {
                watcher?.Dispose();
                var jsonPath = @"Assets/Prefabs/GameObject.prefab.json";
                watcher = renderer.WatchPrefab(jsonPath);
            }

            if (watcher != null && GUILayout.Button("Stop Synchronization"))
            {
                watcher.Dispose();
                watcher = null;
            }

            if (server == null && GUILayout.Button("Start server"))
            {
                server = new Server("http://localhost:8080/", baseDir);
            }

            if (server != null && GUILayout.Button("Stop server"))
            {
                server.Stop();
                server = null;
            }
        }
    }
}

#endif

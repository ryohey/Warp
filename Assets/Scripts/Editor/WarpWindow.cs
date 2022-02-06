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
        private Renderer renderer = new Renderer(new AssetLoader("static"));
        private Server server;

        [MenuItem("Warp/Edit")]
        public static void ShowWindow()
        {
            GetWindow(typeof(WarpWindow));
        }

        void OnGUI()
        {
            var baseDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "static");
            var prefabFilePath = @"Assets/Prefabs/GameObject.prefab";
            var jsonPath = @"static/GameObject.prefab.json";

            if (GUILayout.Button("Convert prefab"))
            {
                var gameObjectElement = Encoder.Encode(prefabFilePath);

                string json = JsonConvert.SerializeObject(gameObjectElement, Formatting.Indented);
                File.WriteAllText(jsonPath, json);
                Debug.Log($"Save Prefab {prefabFilePath} to {jsonPath}");

                Debug.Log("Create AssetBundles...");
                var assetResolver = new AssetResolver("static/");
                assetResolver.CreateDependantAssetBundles(gameObjectElement);
            }

            if (GUILayout.Button("Spawn prefab"))
            {
                var element = Decoder.Decode(jsonPath);
                context = new RenderContext();
                renderer.Spawn(element, null, context);
                renderer.Update(element, context);
            }

            if (context != null && GUILayout.Button("Update prefab"))
            {
                var element = Decoder.Decode(jsonPath);
                renderer.Update(element, context);
            }

            if (context != null && GUILayout.Button("Reconstruct prefab"))
            {
                var element = Decoder.Decode(jsonPath);
                renderer.Reconstruct(element, null, context);
            }

            if (GUILayout.Button("Spawn and Synchronize"))
            {
                watcher?.Dispose();
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

#if UNITY_EDITOR

using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Warp
{
    class WarpWindow : EditorWindow
    {
        private RenderContext context;
        private FileSystemWatcher watcher;
        private Renderer renderer = new Renderer(new AssetLoader("AssetBundle"));

        [MenuItem("Warp/Edit")]
        public static void ShowWindow()
        {
            GetWindow(typeof(WarpWindow));
        }

        private void Awake()
        {
        }

        void OnGUI()
        {

            if (GUILayout.Button("Convert prefab"))
            {
                Encoder.Encode(@"Assets/Prefabs/GameObject.prefab");
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
        }
    }
}

#endif

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
        private SynchronizationContext mainThreadContext;

        [MenuItem("Warp/Edit")]
        public static void ShowWindow()
        {
            GetWindow(typeof(WarpWindow));
        }

        void OnGUI()
        {
            mainThreadContext ??= SynchronizationContext.Current;

            if (GUILayout.Button("Convert prefab"))
            {
                Encoder.Encode(@"Assets/Prefabs/GameObject.prefab");
            }

            if (GUILayout.Button("Spawn prefab"))
            {
                context = Renderer.SpawnPrefab(@"Assets/Prefabs/GameObject.prefab.json");
            }

            if (GUILayout.Button("Update prefab") && context != null)
            {
                Renderer.UpdatePrefab(@"Assets/Prefabs/GameObject.prefab.json", context);
            }

            if (GUILayout.Button("Spawn and Synchronize"))
            {
                watcher?.Dispose();
                var jsonPath = @"Assets/Prefabs/GameObject.prefab.json";
                var context = Renderer.SpawnPrefab(jsonPath);
                watcher = new FileSystemWatcher(Path.GetDirectoryName(jsonPath))
                {
                    Filter = Path.GetFileName(jsonPath),
                    EnableRaisingEvents = true,
                };
                watcher.Changed += (sender, ev) =>
                {
                    Debug.Log($"{ev.ChangeType}: {jsonPath}");

                    DoOnMainThread(() =>
                    {
                        try
                        {
                            Renderer.UpdatePrefab(jsonPath, context);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                        }
                    });
                };
            }

            if (watcher != null && GUILayout.Button("Stop Synchronization"))
            {
                watcher.Dispose();
                watcher = null;
            }
        }

        private void DoOnMainThread(Action action)
        {
            mainThreadContext.Post(__ =>
            {
                action();
            }, null);
        }
    }
}

#endif

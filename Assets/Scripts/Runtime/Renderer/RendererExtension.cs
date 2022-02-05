using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;

namespace Warp
{
    public static class RendererExtension
    {
        public static RenderContext SpawnPrefab(this Renderer self, string jsonPath, Transform parent = null)
        {
            var json = File.ReadAllText(jsonPath);
            var gameObjectElement = JsonConvert.DeserializeObject<GameObjectElement>(json);

            var context = new RenderContext();
            self.Spawn(gameObjectElement, parent, context);
            self.Update(gameObjectElement, context);

            return context;
        }

        public static void UpdatePrefab(this Renderer self, string jsonPath, RenderContext context)
        {
            var json = File.ReadAllText(jsonPath);
            var gameObjectElement = JsonConvert.DeserializeObject<GameObjectElement>(json);
            self.Update(gameObjectElement, context);
        }

        public static FileSystemWatcher WatchPrefab(this Renderer self, string jsonPath, Transform parent = null)
        {
            var mainThreadContext = SynchronizationContext.Current;
            var context = self.SpawnPrefab(jsonPath, parent);
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
                        self.UpdatePrefab(jsonPath, context);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }, null);
            };
            return watcher;
        }

        public static Timer WatchRemotePrefab(this Renderer self, string serverUrl, string prefabName, Transform parent = null)
        {
            var mainThreadContext = SynchronizationContext.Current;
            RenderContext context = null;
            var url = Path.Combine(serverUrl, "static", prefabName);

            var timer = new Timer((_) =>
            {
                try
                {
                    var json = WebRequestUtil.DownloadText(url);
                    var gameObjectElement = JsonConvert.DeserializeObject<GameObjectElement>(json);

                    mainThreadContext.Post(__ =>
                    {
                        try
                        {
                            if (context == null)
                            {
                                context = new RenderContext();
                                self.Spawn(gameObjectElement, parent, context);
                            }
                            else
                            {
                                self.Update(gameObjectElement, context);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                        }
                    }, null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }, null, 0, 3000);

            return timer;
        }
    }
}
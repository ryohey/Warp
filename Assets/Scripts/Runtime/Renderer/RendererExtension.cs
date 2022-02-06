using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;

namespace Warp
{
    public static class RendererExtension
    {
        public static FileSystemWatcher WatchPrefab(this Renderer self, string jsonPath, Transform parent = null)
        {
            var mainThreadContext = SynchronizationContext.Current;

            var gameObjectElement = Decoder.Decode(jsonPath);
            var context = new RenderContext();

            self.Spawn(gameObjectElement, parent, context);
            self.Update(gameObjectElement, context);

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
                        var gameObjectElement = Decoder.Decode(jsonPath);
                        self.Reconstruct(gameObjectElement, parent, context);
                        self.Update(gameObjectElement, context);
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
                                self.Reconstruct(gameObjectElement, parent, context);
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
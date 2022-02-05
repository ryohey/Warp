using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Warp
{
    public class PrefabWatcher : MonoBehaviour
    {
        public string jsonPath;
        public string assetPath;
        private FileSystemWatcher watcher;

        void Start()
        {
            var renderer = new Renderer(new AssetLoader(assetPath));
            watcher = renderer.WatchPrefab(jsonPath, transform);
        }

        private void OnDestroy()
        {
            watcher.Dispose();
        }
    }
}

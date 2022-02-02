using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Warp
{
    public class PrefabWatcher : MonoBehaviour
    {
        public string jsonPath;
        private FileSystemWatcher watcher;

        void Start()
        {
            watcher = Renderer.WatchPrefab(jsonPath, transform);
        }

        private void OnDestroy()
        {
            watcher.Dispose();
        }
    }
}

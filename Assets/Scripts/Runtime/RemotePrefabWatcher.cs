using System.IO;
using System.Threading;
using UnityEngine;

namespace Warp
{
    public class RemotePrefabWatcher : MonoBehaviour
    {
        public string serverUrl;
        public string prefabName;
        private Timer timer;

        void Start()
        {
            var renderer = new Renderer(new RemoteAssetLoader(serverUrl));
            timer = renderer.WatchRemotePrefab(serverUrl, prefabName, transform);
        }

        private void OnDestroy()
        {
            timer.Dispose();
        }
    }
}

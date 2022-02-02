using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Warp
{
    public class PrefabWatcher : MonoBehaviour
    {
        public string jsonPath;

        void Start()
        {
            Renderer.WatchPrefab(jsonPath, transform);
        }
    }
}

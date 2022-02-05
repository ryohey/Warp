using System;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace Warp
{
    public class RemoteAssetLoader : IAssetLoader
    {
        private readonly string baseUrl;

        public RemoteAssetLoader(string baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public T Load<T>(string guid) where T : UnityEngine.Object
        {
            var url = Path.Combine(baseUrl, "static", guid);
            var data = WebRequestUtil.DownloadBytes(url);
            var bundle = AssetBundle.LoadFromMemory(data);
            var assets = bundle.LoadAllAssets<T>();
            if (assets.Length == 0)
            {
                Debug.LogError($"AssetBundle {guid} does not contains Mesh");
            }
            bundle.Unload(false);
            return assets[0];
        }
    }
}

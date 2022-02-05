using System;
using UnityEngine;

namespace Warp
{
    public class AssetLoader
    {
        private readonly string assetPath;

        public AssetLoader(string assetPath)
        {
            this.assetPath = assetPath;
        }

        public T Load<T>(string guid) where T : UnityEngine.Object
        {
            var bundle = AssetBundle.LoadFromFile($"{assetPath}/{guid}");
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

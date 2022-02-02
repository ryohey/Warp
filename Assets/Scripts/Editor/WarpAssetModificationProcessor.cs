using System;
using System.IO;
using System.Linq;

namespace Warp
{
    public class WarpAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            // Get the name of the scene to save.
            string scenePath = string.Empty;
            string sceneName = string.Empty;

            WarpEditor.Instance.willSavePrefabPaths = paths
                .Where(path => path.EndsWith(".prefab"))
                .ToList();

            return paths;
        }
    }
}
using System;
using UnityEditor;

namespace Warp
{
    public class WarpAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssetPaths,
            string[] deletedAssetPaths,
            string[] movedAssetPaths,
            string[] movedFromAssetPaths)
        {
            WarpEditor.Instance.OnPostprocessAllAssets();
        }
    }
}
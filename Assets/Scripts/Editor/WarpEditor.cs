using System;
using System.Collections.Generic;
using System.Linq;

namespace Warp
{
    public class WarpEditor
    {
        public static readonly WarpEditor Instance = new WarpEditor();
        public List<string> willSavePrefabPaths = new List<string>();

        public void OnPostprocessAllAssets()
        {
            var paths = willSavePrefabPaths
                .Where(path => path.StartsWith("Assets/Prefabs/"));

            foreach (var path in paths)
            {
                Encoder.Encode(path);
            }
        }
    }
}

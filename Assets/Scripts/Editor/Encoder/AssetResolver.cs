using System;
using System.Collections.Generic;

namespace Warp
{
    public static class AssetResolver
    {
        public static IDictionary<string, object> ResolveMesh(string fileID)
        {
            return new Dictionary<string, object>
            {
                { "type", "Mesh" },
                { "path", "foobar" }
            };
        }
    }
}

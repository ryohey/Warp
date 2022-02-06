using System;
using System.IO;
using Newtonsoft.Json;

namespace Warp
{
    public static class Decoder
    {
        public static GameObjectElement Decode(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            return JsonConvert.DeserializeObject<GameObjectElement>(json);
        }
    }
}

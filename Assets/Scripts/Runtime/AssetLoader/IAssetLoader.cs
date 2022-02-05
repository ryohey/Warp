using System;

namespace Warp
{
    public interface IAssetLoader
    {
        T Load<T>(string guid) where T : UnityEngine.Object;
    }
}

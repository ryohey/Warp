using System;
using System.Collections.Generic;

namespace Warp
{
    public interface IEncoder
    {
        IDictionary<string, object> Encode();
    }
}

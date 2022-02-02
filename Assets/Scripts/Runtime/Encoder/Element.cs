using System;
using System.Collections.Generic;

namespace Warp
{
    public interface IElement { }

    public struct GameObjectElement : IElement
    {
        public string typeName;
        public string fileID;
        public IDictionary<string, object> properties;
        public IList<ComponentElement> components;
        public IList<GameObjectElement> children;
    }

    public struct ComponentElement : IElement
    {
        public string typeName;
        public int classID;
        public string fileID;
        public IDictionary<string, object> properties;
    }
}

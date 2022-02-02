using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warp
{
    public class RenderContext
    {
        // fileID to instanceID mapping
        public IDictionary<string, int> objectMap = new Dictionary<string, int>();

        public UnityEngine.Object FindObject(string fileID)
        {
            var instanceID = objectMap[fileID];
            return FindGameObject(instanceID) as UnityEngine.Object ?? FindComponent(instanceID);
        }

        public static GameObject FindGameObject(int instanceID)
        {
            return Resources.FindObjectsOfTypeAll(typeof(GameObject))
                   .FirstOrDefault(obj => obj.GetInstanceID() == instanceID) as GameObject;
        }

        public static Component FindComponent(int instanceID)
        {
            return Resources.FindObjectsOfTypeAll(typeof(Component))
                   .FirstOrDefault(obj => obj.GetInstanceID() == instanceID) as Component;
        }
    }
}
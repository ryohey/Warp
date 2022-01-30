#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Warp
{
    class WarpWindow : EditorWindow
    {
        [MenuItem("Warp/Edit")]
        public static void ShowWindow()
        {
            GetWindow(typeof(WarpWindow));
        }

        void OnGUI()
        {
            if (GUILayout.Button("Convert prefab"))
            {
                Encoder.Encode(@"Assets/Prefabs/GameObject.prefab");
            }
        }
    }
}

#endif

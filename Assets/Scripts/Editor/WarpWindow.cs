#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Warp
{
    class WarpWindow : EditorWindow
    {

        private RenderContext context;

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

            if (GUILayout.Button("Spawn prefab"))
            {
                context = Renderer.SpawnPrefab(@"Assets/Prefabs/GameObject.prefab.json");
            }

            if (GUILayout.Button("Update prefab") && context != null)
            {
                Renderer.UpdatePrefab(@"Assets/Prefabs/GameObject.prefab.json", context);
            }
        }
    }
}

#endif

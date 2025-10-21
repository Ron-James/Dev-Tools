#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _Assets.Scripts.ScriptableObjectMaagement.Editor
{
    public static class ScriptableObjectManagerEditor
    {
        [MenuItem("Tools/Guid Assets/Select Manager Asset", priority = 0)]
        public static void SelectManager()
        {
            var instance = ScriptableObjectManagerAsset.Instance;
            if (instance != null)
            {
                Selection.activeObject = instance;
                EditorGUIUtility.PingObject(instance);
            }
            else
            {
                Debug.LogError("ScriptableObjectManagerAsset instance could not be created/located.");
            }
        }

        [MenuItem("Tools/Guid Assets/Refresh Registry", priority = 1)]
        public static void RefreshRegistry()
        {
            var instance = ScriptableObjectManagerAsset.Instance;
            if (instance != null)
            {
                instance.RefreshRegistry();
                Debug.Log("Guid registry refreshed.");
            }
            else
            {
                Debug.LogError("ScriptableObjectManagerAsset instance could not be created/located.");
            }
        }
    }
}
#endif

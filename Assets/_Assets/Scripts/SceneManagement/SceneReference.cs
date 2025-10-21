using System;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace CustomSceneReference
{
    [Serializable]
    public class SceneReference
    {
        [ValueDropdown("GetSceneList")] [SerializeField]
        private string scenePath;

        public string ScenePath => scenePath;

        public SceneReference(string path)
        {
            this.scenePath = path;
        }

        [Button]
        public void LoadScene()
        {
            if (Application.isPlaying)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(scenePath);
            }
        }

        // For direct serialization usage, you might also want an empty constructor:
        public SceneReference()
        {
        }

#if UNITY_EDITOR
        private static string[] GetSceneList()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }
#endif
    }
}
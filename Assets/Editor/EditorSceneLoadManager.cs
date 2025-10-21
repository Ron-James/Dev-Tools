#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EditorPlayModeSceneReloader
{
    static EditorPlayModeSceneReloader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        // When exiting edit mode (i.e. about to enter play mode), reload the current scene.
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            
            // Optionally save the scene if there are unsaved changes.
            if (activeScene.isDirty)
            {
                EditorSceneManager.SaveScene(activeScene);
            }
            Debug.Log("Opening scene: " + activeScene.path);
            EditorSceneManager.OpenScene(activeScene.path);
        }
        
    }
}
#endif

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace TutorialInfo.Scripts.Editor
{
    public class ScriptableObjectPanel : OdinEditorWindow
    {
        // The ScriptableObject that this panel will display.
        [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
        public ScriptableObject targetScriptableObject;

        [MenuItem("Window/Scriptable Object/Panel")]
        private static void OpenWindow()
        {
            // Opens the panel without a specific reference.
            var window = GetWindow<ScriptableObjectPanel>("Scriptable Object Panel");
            window.Show();
        }

        /// <summary>
        /// Opens the panel and loads the provided ScriptableObject.
        /// </summary>
        /// <param name="so">The ScriptableObject to display in the panel.</param>
        public static void OpenPanelForScriptableObject(ScriptableObject so)
        {
            var window = GetWindow<ScriptableObjectPanel>("Scriptable Object Panel");
            window.targetScriptableObject = so;
            // Update the window title based on the ScriptableObject's name.
            if (so != null)
            {
                window.titleContent = new GUIContent(so.name);
            }
            else
            {
                window.titleContent = new GUIContent("Scriptable Object Panel");
            }
            window.Show();
        }

        protected override void OnImGUI()
        {
            // Display an object field to allow drag and drop.
            targetScriptableObject = EditorGUILayout.ObjectField("Scriptable Object", targetScriptableObject, typeof(ScriptableObject), false) as ScriptableObject;
        
            // Dynamically update the window title based on the target's name.
            if (targetScriptableObject != null)
            {
                titleContent = new GUIContent(targetScriptableObject.name);
            }
            else
            {
                titleContent = new GUIContent("Scriptable Object Panel");
            }
        
            // Draw the inline inspector if a ScriptableObject is assigned.
            if (targetScriptableObject != null)
            {
                base.OnImGUI();
            }
        }
    }
}

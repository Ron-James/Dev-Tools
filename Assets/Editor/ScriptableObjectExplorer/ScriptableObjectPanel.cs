using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor.ScriptableObjectExplorer
{
    public class ScriptableObjectPanel : OdinEditorWindow
    {
        // The ScriptableObject that this panel will display.
        [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
        public ScriptableObject targetScriptableObject;

        [MenuItem("Window/Scriptable Object/Panel")]
        private static void OpenWindow()
        {
            // Opens the panel without a specific reference
            GetWindow<ScriptableObjectPanel>("Scriptable Object Panel").Show();
        }

        /// <summary>
        /// Opens the panel and loads the provided ScriptableObject.
        /// </summary>
        /// <param name="so">The ScriptableObject to display in the panel.</param>
        public static void OpenPanelForScriptableObject(ScriptableObject so)
        {
            // Get or create the panel window
            var window = GetWindow<ScriptableObjectPanel>(so.name + " Panel");
            // Set the target ScriptableObject
            window.targetScriptableObject = so;
            // Bring the window to focus and show it
            window.Show();
        }

        protected override void OnImGUI()
        {
            // Display an object field for drag and drop if needed
            targetScriptableObject = EditorGUILayout.ObjectField("Scriptable Object", targetScriptableObject, typeof(ScriptableObject), false) as ScriptableObject;

            // If a ScriptableObject is assigned, draw its inline inspector
            if (targetScriptableObject != null)
            {
                base.OnImGUI();
            }
        }
    }
}
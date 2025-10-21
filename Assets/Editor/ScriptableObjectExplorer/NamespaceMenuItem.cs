using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;



namespace ScriptsbleObjectExplorer
{
    /// <summary>
    /// A custom OdinMenuItem that draws a folder icon for namespace nodes.
    /// </summary>
    public class NamespaceMenuItem : OdinMenuItem
    {
        private readonly Texture2D folderIcon;

        public NamespaceMenuItem(OdinMenuTree tree, string name, object value) : base(tree, name, value)
        {
            folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
        } 

        /// <summary>
        /// Draws the menu item with a custom folder icon.
        /// </summary>
        public override void DrawMenuItem(int indentLevel)
        {
            Rect rect = this.Rect;

            // Calculate the icon's position
            var iconRect = new Rect(rect.x + 15 + (indentLevel * 15), rect.y + (rect.height - 16) / 2, 16, 16);

            // Draw the folder icon
            if (folderIcon != null)
            {
                GUI.DrawTexture(iconRect, folderIcon, ScaleMode.ScaleToFit);
            }

            // Draw the menu item name next to the icon
            var labelRect = new Rect(iconRect.xMax + 5, rect.y, rect.width - iconRect.xMax - 5, rect.height);
            GUI.Label(labelRect, this.Name, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft });
        }
    }
}
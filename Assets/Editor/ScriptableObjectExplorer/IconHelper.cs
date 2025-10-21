using System;
using UnityEditor;
using UnityEngine;

public static class IconHelper
{
    /// <summary>
    /// Returns the default icon for a given ScriptableObject type as a Texture2D.
    /// </summary>
    public static Texture2D GetDefaultIconForScriptableObject(Type type)
    {
        //ensure type is a ScriptableObject
        if (!typeof(ScriptableObject).IsAssignableFrom(type))
        {
            Debug.LogError("Type is not a ScriptableObject.");
            return null;
        }
        
        
        // ObjectContent(null, typeof(T)) tells Unity to get the default icon
        // for the type T (since we didn't provide an actual object instance)
        GUIContent content = EditorGUIUtility.ObjectContent(null, type);
        
        // content.image is a Texture (cast it to Texture2D if needed)
        return content.image as Texture2D;
    }
}
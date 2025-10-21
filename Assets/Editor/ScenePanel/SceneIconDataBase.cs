#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using CustomSceneReference;

[CreateAssetMenu(fileName = "SceneIconDatabase", menuName = "Scene Icon Database")]
public class SceneIconDatabase : ScriptableObject
{
    // A default icon for any newly added scene that doesn't have a custom icon yet
    [Title("Database Settings")]
    [PreviewField(64, ObjectFieldAlignment.Center)]
    [SerializeField] private Texture2D defaultIcon;

    // A table showing each scene reference + icon
    [TableList(AlwaysExpanded = true)]
    [SerializeReference] private List<BuildSceneReference> buildScenes = new List<BuildSceneReference>();

    /// <summary>
    /// When the asset is loaded or reloaded, subscribe to sceneListChanged and sync.
    /// </summary>
    private void OnEnable()
    {
#if UNITY_EDITOR
        UpdateSceneList();
        EditorBuildSettings.sceneListChanged += UpdateSceneList;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorBuildSettings.sceneListChanged -= UpdateSceneList;
#endif
    }

    /// <summary>
    /// Ensures this list matches the current enabled scenes in Build Settings.
    /// </summary>
    [Button(ButtonSizes.Medium)]
    public void UpdateSceneList()
    {
#if UNITY_EDITOR
        // Get all *enabled* scenes from Build Settings
        var enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .ToList();

        // Build a new list by pulling existing icons from the old list
        var newList = new List<BuildSceneReference>();
        foreach (var scene in enabledScenes)
        {
            // If this path already exists, grab its old icon; otherwise, default.
            var existing = buildScenes.FirstOrDefault(x => x.ScenePath == scene.path);
            Texture2D iconToUse = existing != null && existing.Icon != null 
                ? existing.Icon 
                : defaultIcon;

            var reference = new BuildSceneReference(scene.path, iconToUse);
            newList.Add(reference);
        }

        buildScenes = newList;
        EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Looks up a custom icon for the specified scene path, or returns the default if not found.
    /// </summary>
    public Texture2D GetIcon(string path)
    {
        var match = buildScenes.FirstOrDefault(x => x.ScenePath == path);
        if (match != null && match.Icon != null)
        {
            return match.Icon;
        }
        return defaultIcon;
    }

    public List<BuildSceneReference> BuildScenes => buildScenes;
}

/// <summary>
/// An extension of SceneReference that also stores a custom icon.
/// Marked [Serializable] so it properly serializes in the list.
/// </summary>
[System.Serializable]
public class BuildSceneReference : SceneReference
{
    // Using Odin’s [PreviewField] to let you pick an icon from the Project
    [TableColumnWidth(80, false)]
    [PreviewField(64, ObjectFieldAlignment.Left), HideLabel]
    [SerializeField] private Texture2D icon;

    public Texture2D Icon => icon;

    public BuildSceneReference(string scenePath, Texture2D icon) : base(scenePath)
    {
        this.icon = icon;
    }

    // You need a parameterless constructor for Unity to serialize this class cleanly.
    public BuildSceneReference() : base(string.Empty) { }
}
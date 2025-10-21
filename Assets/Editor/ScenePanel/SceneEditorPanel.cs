#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Sirenix.OdinInspector.Editor;

public class SceneEditorPanel : OdinEditorWindow
{
    [MenuItem("Tools/Scene Panel")]
    private static void OpenWindow()
    {
        GetWindow<SceneEditorPanel>("Scene Panel").Show();
    }

    // A helper struct/class to store references for the panel
    private class SceneButton
    {
        public string Path { get; }
        public string Name { get; }
        private SceneIconDatabase database;

        public SceneButton(string path, SceneIconDatabase db)
        {
            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
            database = db;
        }

        public Texture2D GetThumbnail()
        {
            if (database == null) return null;
            return database.GetIcon(Path);
        }

        public void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(Path, OpenSceneMode.Single);
            }
        }
    }

    [SerializeField, HideIf("hasDatabase")]
    private SceneIconDatabase sceneDatabase; // Assign in Inspector or via code

    private Vector2 scrollPosition;
    private List<SceneButton> scenes;
    private bool hasDatabase => sceneDatabase != null;

    protected override void OnEnable()
    {
        // If no database is assigned, you might attempt to auto-locate one:
        // if (!sceneDatabase)
        // {
        //     sceneDatabase = AssetDatabase.LoadAssetAtPath<SceneIconDatabase>("Assets/SceneIconDatabase.asset");
        // }
        base.OnEnable();
        RefreshSceneList();
        EditorBuildSettings.sceneListChanged += RefreshSceneList;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EditorBuildSettings.sceneListChanged -= RefreshSceneList;
    }
    [Button]
    private void RefreshSceneList()
    {
        // If we have a database, ensure it’s up to date
        if (sceneDatabase)
        {
            sceneDatabase.UpdateSceneList();
        }

        // Build local list of enabled scenes
        scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => new SceneButton(s.path, sceneDatabase))
            .ToList();

        Repaint();
    }

    protected override void OnImGUI()
    {
        base.OnImGUI();

        // In case no database is assigned
        if (!sceneDatabase)
        {
            EditorGUILayout.HelpBox("No SceneIconDatabase assigned! Please create or assign one.", MessageType.Warning);
            if (GUILayout.Button("Create SceneIconDatabase"))
            {
                // Optionally auto-create
                sceneDatabase = ScriptableObject.CreateInstance<SceneIconDatabase>();
                AssetDatabase.CreateAsset(sceneDatabase, "Assets/SceneIconDatabase.asset");
                AssetDatabase.SaveAssets();
            }
            return;
        }

        GUILayout.Label("Scenes", EditorStyles.boldLabel);
        

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // DYNAMIC LAYOUT
        float panelWidth = EditorGUIUtility.currentViewWidth - 30f;
        const float minTileWidth = 150f;
        int columns = Mathf.FloorToInt(panelWidth / minTileWidth);
        columns = Mathf.Max(1, columns);

        float cellWidth = panelWidth / columns;
        float tileAspectRatio = 1.25f; 
        float cellHeight = cellWidth * tileAspectRatio;
        float thumbnailHeight = cellHeight * 0.75f;
        float labelHeight = cellHeight * 0.25f;

        GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontStyle = FontStyle.Bold
        };

        for (int i = 0; i < scenes.Count; i++)
        {
            if (i % columns == 0)
            {
                EditorGUILayout.BeginHorizontal();
            }

            var scene = scenes[i];
            EditorGUILayout.BeginVertical(GUILayout.Width(cellWidth), GUILayout.Height(cellHeight));

            // (1) Thumbnail Button
            var thumb = scene.GetThumbnail();
            if (GUILayout.Button(thumb, GUILayout.Width(cellWidth), GUILayout.Height(thumbnailHeight)))
            {
                scene.OpenScene();
            }

            // (2) Scene Name
            Rect labelRect = GUILayoutUtility.GetRect(cellWidth, labelHeight);
            GUI.Label(labelRect, scene.Name, labelStyle);

            EditorGUILayout.EndVertical();

            if (i % columns == columns - 1)
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        if (scenes.Count % columns != 0)
        {
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif

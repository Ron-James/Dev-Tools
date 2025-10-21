// UnityEventDebugger.cs
// Odin Inspector-powered tool to debug and locate UnityEvent references
// across scene objects, ScriptableObjects, and prefabs.
// Includes prefab asset reference tracking and prefab view navigation.
// Place this in an Editor folder.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Object = UnityEngine.Object;

#region Data Structures

/// <summary>
/// Represents a persistent UnityEvent binding to a target + method.
/// </summary>
[Serializable]
public class UnityEventBinding
{
    [ReadOnly] public Component SourceComponent;
    [ReadOnly] public string FieldName;
    [ReadOnly] public UnityEngine.Object Target;
    [ReadOnly] public string MethodName;
    [ReadOnly] public string Error;

    public override string ToString() =>
        $"{SourceComponent?.GetType().Name}.{FieldName} → {Target?.name}.{MethodName}";
}

/// <summary>
/// Groups all bindings of a UnityEvent field, and tracks the source object.
/// If the source is from a prefab, also tracks the prefab asset.
/// </summary>
[Serializable]
public class UnityEventEntry
{
    [ReadOnly] public UnityEngine.Object SourceObject;
    [ReadOnly] public string ComponentName;
    [ReadOnly] public string FieldName;

    [ReadOnly, ShowInInspector] public GameObject PrefabAsset; // Pointer to prefab asset, if from prefab

    [TableList(AlwaysExpanded = true), ReadOnly]
    public List<UnityEventBinding> Bindings = new();
}

#endregion

#region Scanner

/// <summary>
/// Scans all UnityEvents in the current scene, ScriptableObjects, and prefabs,
/// and collects those that reference any of the given target objects.
/// </summary>
public static class UnityEventScanner
{
    public static List<UnityEventEntry> FindReferencesTo(List<UnityEngine.Object> targets)
    {
        var results = new List<UnityEventEntry>();
        var targetSet = new HashSet<UnityEngine.Object>(targets);

        ScanSceneObjects(targetSet, results);
        ScanScriptableObjects(targetSet, results);
        ScanPrefabs(targetSet, results);

        return results;
    }

    private static void ScanSceneObjects(HashSet<UnityEngine.Object> targets, List<UnityEventEntry> results)
    {
        foreach (var component in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (component == null) continue;
            ScanComponentFields(component, targets, results);
        }
    }

    private static void ScanScriptableObjects(HashSet<UnityEngine.Object> targets, List<UnityEventEntry> results)
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so == null) continue;

            var fields = so.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (!typeof(UnityEventBase).IsAssignableFrom(field.FieldType)) continue;
                if (!(field.GetValue(so) is UnityEventBase unityEvent)) continue;

                var entry = new UnityEventEntry
                {
                    SourceObject = so,
                    ComponentName = so.GetType().Name,
                    FieldName = field.Name
                };

                AddMatchingBindings(unityEvent, field.Name, null, targets, entry.Bindings);

                if (entry.Bindings.Count > 0)
                    results.Add(entry);
            }
        }
    }

    private static void ScanPrefabs(HashSet<UnityEngine.Object> targets, List<UnityEventEntry> results)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null) continue;

            foreach (var component in prefabRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;

                var fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    if (!typeof(UnityEventBase).IsAssignableFrom(field.FieldType)) continue;
                    if (!(field.GetValue(component) is UnityEventBase unityEvent)) continue;

                    var entry = new UnityEventEntry
                    {
                        SourceObject = component,
                        ComponentName = component.GetType().Name + $" (Prefab: {prefabAsset.name})",
                        FieldName = field.Name,
                        PrefabAsset = prefabAsset
                    };

                    AddMatchingBindings(unityEvent, field.Name, component, targets, entry.Bindings);

                    if (entry.Bindings.Count > 0)
                        results.Add(entry);
                }
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ScanComponentFields(Component component, HashSet<UnityEngine.Object> targets, List<UnityEventEntry> results, string contextSuffix = "")
    {
        var fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (!typeof(UnityEventBase).IsAssignableFrom(field.FieldType)) continue;
            if (!(field.GetValue(component) is UnityEventBase unityEvent)) continue;

            var entry = new UnityEventEntry
            {
                SourceObject = component,
                ComponentName = component.GetType().Name + (string.IsNullOrEmpty(contextSuffix) ? "" : $" {contextSuffix}"),
                FieldName = field.Name
            };

            AddMatchingBindings(unityEvent, field.Name, component, targets, entry.Bindings);

            if (entry.Bindings.Count > 0)
                results.Add(entry);
        }
    }

    private static void AddMatchingBindings(UnityEventBase unityEvent, string fieldName, Component source, HashSet<UnityEngine.Object> targets, List<UnityEventBinding> output)
    {
        int count = unityEvent.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            var persistentTarget = unityEvent.GetPersistentTarget(i);
            if (persistentTarget == null) continue;

            bool isMatch =
                targets.Contains(persistentTarget) ||
                (persistentTarget is Component comp && targets.Contains(comp.gameObject));

            if (!isMatch) continue;

            output.Add(new UnityEventBinding
            {
                SourceComponent = source,
                FieldName = fieldName,
                Target = persistentTarget,
                MethodName = unityEvent.GetPersistentMethodName(i),
                Error = string.IsNullOrEmpty(unityEvent.GetPersistentMethodName(i)) ? "Missing method or target" : null
            });
        }
    }
}

#endregion

#region Custom Drawer

/// <summary>
/// Custom Odin drawer for UnityEventEntry. Draws the actual UnityEvent using SerializedObject,
/// and provides tools to ping or open the prefab asset and locate the object in Prefab Mode.
/// </summary>
public class UnityEventEntryDrawer : OdinValueDrawer<UnityEventEntry>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var entry = this.ValueEntry.SmartValue;

        if (entry.SourceObject == null || string.IsNullOrEmpty(entry.FieldName))
        {
            SirenixEditorGUI.WarningMessageBox("Invalid entry — no source or field name.");
            return;
        }

        EditorGUILayout.LabelField($"{entry.ComponentName}.{entry.FieldName}", EditorStyles.boldLabel);

        var serializedObject = new SerializedObject(entry.SourceObject);
        var property = serializedObject.FindProperty(entry.FieldName);

        if (property != null)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, true);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
        else
        {
            SirenixEditorGUI.WarningMessageBox($"Could not find serialized field '{entry.FieldName}'");
        }

        if (entry.PrefabAsset != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Prefab Asset", GUILayout.Width(150)))
            {
                EditorGUIUtility.PingObject(entry.PrefabAsset);
            }
            if (GUILayout.Button("Open Prefab", GUILayout.Width(150)))
            {
                AssetDatabase.OpenAsset(entry.PrefabAsset);
            }
            GUILayout.EndHorizontal();

            if (entry.SourceObject is Component comp)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
                {
                    foreach (var t in stage.prefabContentsRoot.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == comp.name && t.GetComponent(comp.GetType()) != null)
                        {
                            if (GUILayout.Button("Ping in Prefab View"))
                            {
                                Selection.activeGameObject = t.gameObject;
                                EditorGUIUtility.PingObject(t.gameObject);
                            }
                            break;
                        }
                    }
                }
            }
        }

        GUILayout.Space(5);
        this.CallNextDrawer(label); // Draw bindings table
    }
}

#endregion

#region Odin Window

/// <summary>
/// Main OdinEditorWindow for scanning and viewing UnityEvent references.
/// </summary>
public class UnityEventDebugWindow : OdinEditorWindow
{
    [MenuItem("Tools/Odin/UnityEvent Debugger")]
    private static void OpenWindow() => GetWindow<UnityEventDebugWindow>("UnityEvent Debugger").Show();

    [InfoBox("Drag GameObjects, Components, or ScriptableObjects here to find all UnityEvents referencing them across the scene, prefabs, and assets.")]
    [ListDrawerSettings(Expanded = true)]
    [SerializeField] private List<Object> objectsToScan = new();

    [Button("Find UnityEvents Referencing Assigned Objects")]
    private void FindReferencesToTargets()
    {
        report.Clear();
        report.AddRange(UnityEventScanner.FindReferencesTo(objectsToScan));

        if (report.Count == 0)
            Debug.Log("No UnityEvents found referencing the assigned objects.");

        GUIUtility.keyboardControl = 0;
        Repaint();
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    [ShowInInspector, TableList(AlwaysExpanded = true)]
    [ReadOnly]
    private List<UnityEventEntry> report = new();
}

#endregion

#endif

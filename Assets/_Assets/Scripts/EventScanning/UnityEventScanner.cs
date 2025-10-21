using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Represents one persistent UnityEvent binding (target + method).
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
/// Groups all bindings of a single UnityEvent field.
/// </summary>
[Serializable]
public class UnityEventEntry
{
    [ReadOnly] public UnityEngine.Object SourceObject;
    [ReadOnly] public string ComponentName;
    [ReadOnly] public string FieldName;

    [TableList(AlwaysExpanded = true), ReadOnly]
    public List<UnityEventBinding> Bindings = new();
}

public static class UnityEventScanner
{
    public static List<UnityEventEntry> FindReferencesTo(List<UnityEngine.Object> targets)
    {
        var results = new List<UnityEventEntry>();
        var targetSet = new HashSet<UnityEngine.Object>(targets);

        ScanSceneObjects(targetSet, results);
#if UNITY_EDITOR
        ScanScriptableObjects(targetSet, results);
        ScanPrefabs(targetSet, results);
#endif
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

#if UNITY_EDITOR
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
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null) continue;

            foreach (var component in prefabRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;

                ScanComponentFields(component, targets, results, $"(Prefab: {System.IO.Path.GetFileNameWithoutExtension(path)})");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
#endif

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// ScriptableObject-based singleton manager for GUID-based ScriptableObject lookup.
/// Ensures a single instance exists. At runtime, will load from Resources or create a hidden temporary instance.
/// In the Editor, if no asset is found, it auto-creates one at Assets/Resources/ScriptableObjectManager.asset.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-10000)]
[CreateAssetMenu(menuName = "Managers/ScriptableObject Manager", fileName = "ScriptableObjectManager")]
public sealed class ScriptableObjectManagerAsset : SerializedScriptableObject, IGuidAssetLookup
{
    private const string DefaultResourcesAssetName = "ScriptableObjectManager";
    private const string DefaultResourcesPath = "Assets/Resources";
    private const string DefaultAssetPath = DefaultResourcesPath + "/" + DefaultResourcesAssetName + ".asset";

    private static ScriptableObjectManagerAsset _instance;

    // Runtime registry is not serialized; it is rebuilt on load/enter play.
    [NonSerialized]
    private Dictionary<Type, Dictionary<string, ScriptableObject>> _registry = new();

    [NonSerialized]
    private bool _initialized;

    [ShowInInspector, ReadOnly]
    [PropertyOrder(-1)]
    [LabelText("Registered Assets (by Type ⇒ Guid)")]
    [DictionaryDrawerSettings(IsReadOnly = true, KeyLabel = "Type", ValueLabel = "Guid → Asset")] 
    private Dictionary<string, Dictionary<string, ScriptableObject>> RegistryView
    {
        get
        {
            var view = new Dictionary<string, Dictionary<string, ScriptableObject>>();
            foreach (var (type, map) in _registry)
            {
                var typeKey = type.FullName ?? type.Name ?? "(UnnamedType)";
                view[typeKey] = map != null ? new Dictionary<string, ScriptableObject>(map) : new Dictionary<string, ScriptableObject>();
            }
            return view;
        }
    }

    [Button(ButtonSizes.Medium), PropertyOrder(-2)]
    private void Refresh() => RefreshRegistry();

    public static ScriptableObjectManagerAsset Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            // Try Resources (any folder under Resources)
            var fromResources = Resources.LoadAll<ScriptableObjectManagerAsset>(string.Empty).FirstOrDefault();
            if (fromResources != null)
            {
                _instance = fromResources;
                return _instance;
            }

#if UNITY_EDITOR
            // Try to find existing asset anywhere in project via AssetDatabase
            var found = FindOrCreateInEditor();
            if (found != null)
            {
                _instance = found;
                return _instance;
            }
#endif
            // Fallback: create a temporary runtime instance (not saved)
            _instance = CreateInstance<ScriptableObjectManagerAsset>();
            _instance.hideFlags = HideFlags.HideAndDontSave;
            _instance.BuildOrRefreshRegistry();
            return _instance;
        }
    }

#if UNITY_EDITOR
    private static ScriptableObjectManagerAsset FindOrCreateInEditor()
    {
        // Try to find by exact default path first
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjectManagerAsset>(DefaultAssetPath);
        if (asset != null)
            return asset;

        // Else search by type anywhere
        var guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObjectManagerAsset");
        if (guids != null && guids.Length > 0)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var found = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjectManagerAsset>(path);
            if (found != null) return found;
        }

        // Create a new one under Assets/Resources
        if (!Directory.Exists(DefaultResourcesPath))
        {
            Directory.CreateDirectory(DefaultResourcesPath);
            UnityEditor.AssetDatabase.Refresh();
        }

        var created = CreateInstance<ScriptableObjectManagerAsset>();
        UnityEditor.AssetDatabase.CreateAsset(created, DefaultAssetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        created.BuildOrRefreshRegistry();
        return created;
    }
#endif

    private void OnEnable()
    {
        hideFlags &= ~HideFlags.DontUnloadUnusedAsset; // allow unload if not referenced in editor; instance keeps a static ref anyway
        BuildOrRefreshRegistry();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        _ = Instance; // triggers creation and registry build
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void EnsureInstanceInEditor()
    {
        _ = Instance; // make registry ready for inspectors in Editor
    }
#endif

    public void RefreshRegistry()
    {
        _initialized = false;
        BuildOrRefreshRegistry();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        BuildOrRefreshRegistry();
    }

    private void BuildOrRefreshRegistry()
    {
        _registry.Clear();
        LoadAllAssets();
        _initialized = true;
    }

    public void LoadAllAssets()
    {
        var allAssets = Resources.LoadAll<ScriptableObject>(string.Empty);
        foreach (var asset in allAssets)
        {
            if (asset is not IGuidAsset guidAsset)
                continue;

            if (string.IsNullOrEmpty(guidAsset.Guid))
            {
                guidAsset.AssignGuid();
            }

            var type = asset.GetType();
            if (!_registry.TryGetValue(type, out var map))
            {
                map = new Dictionary<string, ScriptableObject>();
                _registry[type] = map;
            }

            if (!string.IsNullOrEmpty(guidAsset.Guid))
            {
                if (!map.ContainsKey(guidAsset.Guid))
                {
                    map[guidAsset.Guid] = asset;
                }
                else if (!ReferenceEquals(map[guidAsset.Guid], asset))
                {
                    Debug.LogWarning($"Duplicate GUID detected: {guidAsset.Guid} on asset {asset.name}");
                }
            }
        }
    }

    public ScriptableObject GetAssetByGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;
        EnsureInitialized();
        foreach (var kvp in _registry)
        {
            if (kvp.Value.TryGetValue(guid, out var asset))
                return asset;
        }
        return null;
    }

    public IEnumerable<T> AllOfType<T>() where T : IGuidAsset
    {
        EnsureInitialized();
        foreach (var (type, map) in _registry)
        {
            if (!typeof(T).IsAssignableFrom(type)) continue;
            foreach (var value in map.Values)
            {
                if (value is T typed)
                    yield return typed;
            }
        }
    }

    T IGuidAssetLookup.GetAssetByGuid<T>(string guid)
    {
        throw new NotImplementedException();
    }

    public T GetAssetByGuid<T>(string guid) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(guid)) return null;
        EnsureInitialized();
        foreach (var (type, map) in _registry)
        {
            if (!typeof(T).IsAssignableFrom(type)) continue;
            if (map.TryGetValue(guid, out var asset))
                return asset as T;
        }
        return null;
    }

    public bool TryGetAssetByGuid<T>(string guid, out T asset) where T : ScriptableObject
    {
        asset = GetAssetByGuid<T>(guid);
        return asset != null;
    }

    public T[] GetAssetsByType<T>() where T : ScriptableObject, IGuidAsset
    {
        EnsureInitialized();
        var assets = new List<T>();
        foreach (var (type, map) in _registry)
        {
            if (!typeof(T).IsAssignableFrom(type)) continue;
            foreach (var value in map.Values)
            {
                if (value is T typed)
                    assets.Add(typed);
            }
        }
        return assets.ToArray();
    }

    public T GetFirstAssetOfType<T>() where T : ScriptableObject
    {
        EnsureInitialized();
        foreach (var (type, map) in _registry)
        {
            if (!typeof(T).IsAssignableFrom(type)) continue;
            return map.Values.FirstOrDefault() as T;
        }
        return null;
    }

    public void Register(IGuidAsset guidAsset)
    {
        if (guidAsset == null) return;
        if (string.IsNullOrEmpty(guidAsset.Guid))
        {
            guidAsset.AssignGuid();
        }
        if (guidAsset is not ScriptableObject so) return;

        var type = so.GetType();
        if (!_registry.TryGetValue(type, out var map))
        {
            map = new Dictionary<string, ScriptableObject>();
            _registry[type] = map;
        }
        map[guidAsset.Guid] = so;
        _initialized = true;
    }

    public ScriptableObject[] All()
    {
        EnsureInitialized();
        return _registry.Values.SelectMany(x => x.Values).ToArray();
    }
}

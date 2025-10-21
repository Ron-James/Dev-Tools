using System;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

// Centralized GUID types and GuidReference that uses an Odin-serialized lookup dependency.

public interface IGuidAsset : IEquatable<IGuidAsset>
{
    string Guid { get; }
    void AssignGuid();
}

// Lookup interface to decouple callers from concrete manager implementation.
public interface IGuidAssetLookup
{
    ScriptableObject GetAssetByGuid(string guid);
    T GetAssetByGuid<T>(string guid) where T : ScriptableObject;
    bool TryGetAssetByGuid<T>(string guid, out T asset) where T : ScriptableObject;
    ScriptableObject[] All();
}

public interface IGuidReference
{
    string Guid { get; set; }
    IGuidAsset Asset { get; }
}

public class GuidReference : IGuidReference
{
    [FormerlySerializedAs("guid")]
    [SerializeField, ReadOnly] private string _guid;

    // Odin-serialized lookup dependency. Assign the ScriptableObjectManagerAsset (it implements IGuidAssetLookup).
    [OdinSerialize] private IGuidAssetLookup _lookup;

    [NonSerialized] private IGuidAsset _asset;

    public string Guid
    {
        get => _guid;
        set
        {
            if (_guid == value) return;
            _guid = value;
            _asset = null; // force re-resolve on next access
        }
    }
    
    [Button, GUIColor("green")]
    public void SetAsset(IGuidAsset asset)
    {
        _asset = asset;
        _guid = asset != null ? asset.Guid : null;
    }

    [ShowInInspector, ReadOnly]
    public IGuidAsset Asset
    {
        get
        {
            if (_asset == null && !string.IsNullOrEmpty(_guid))
            {
                var lookup = EnsureLookup();
                if (lookup != null)
                {
                    _asset = lookup.GetAssetByGuid(_guid) as IGuidAsset;
                }
            }
            return _asset;
        }
        set
        {
            _asset = value;
            _guid = value != null ? value.Guid : null;
        }
    }

    // Utility: typed resolve
    public T Resolve<T>() where T : ScriptableObject, IGuidAsset
    {
        if (_asset is T typed) return typed;
        if (string.IsNullOrEmpty(_guid)) return null;
        var lookup = EnsureLookup();
        typed = lookup != null ? lookup.GetAssetByGuid<T>(_guid) : null;
        _asset = typed;
        return typed;
    }

    private IGuidAssetLookup EnsureLookup()
    {
        if (_lookup != null) return _lookup;
        // Attempt to auto-resolve from Resources without using the removed static facade.
        var found = Resources.LoadAll<ScriptableObjectManagerAsset>(string.Empty).FirstOrDefault();
        if (found != null)
        {
            _lookup = found;
            return _lookup;
        }
        // Fallback to singleton instance if available (kept for robustness in editor/runtime).
        _lookup = ScriptableObjectManagerAsset.Instance;
        return _lookup;
    }
}

public abstract class SerializableScriptableObject : SerializedScriptableObject, IGuidAsset
{
    [OdinSerialize]
    private Guid _cachedGuid; // serialized via Odin

    // Empty string if not yet assigned (so inspectors can detect missing state)
    [ShowInInspector, ReadOnly]
    public string Guid => _cachedGuid == default ? string.Empty : _cachedGuid.ToString();

    private void OnValidate()
    {
        // Assign once if missing; avoid changing existing GUIDs
        if (_cachedGuid == default)
        {
            AssignGuid();
        }
    }
    
    public void AssignGuid()
    {
        if (_cachedGuid != default) return; // assign only once
        _cachedGuid = System.Guid.NewGuid();
#if UNITY_EDITOR
        // Persist in editor
        if (this)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    public bool Equals(IGuidAsset other)
    {
        if (other == null) return false;
        return this.Guid == other.Guid;
    }
}


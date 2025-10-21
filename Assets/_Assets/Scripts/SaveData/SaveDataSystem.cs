using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Threading.Tasks;
using UnityEngine.Serialization;
using System.Linq;

public interface ISaveable : IGuidAsset
{
    // If false, this asset is ignored by the save system (useful for debug or temporary data)
    bool SaveDataEnabled { get; }

    // Caller captures a dictionary of key/value pairs representing the asset's state.
    // Keys should be stable identifiers for fields or logical chunks (e.g., "Health", "UnlockedLevels").
    Dictionary<string, object> CaptureState();

    // Restore state previously captured by CaptureState. Missing keys should be handled gracefully by the asset.
    void RestoreState(Dictionary<string, object> state);
}

public interface ISaveData
{
    string SaveName { get; }
    DateTime LastSaveTime { get; }

    // Mapping: GUID string of a saveable asset -> that asset's state dictionary
    Dictionary<string, Dictionary<string, object>> DataBindings { get; }

    // Apply captured data back into the corresponding ScriptableObject assets.
    void ApplyData();

    // Capture state from a collection of ISaveable assets into this save slot.
    void CaptureData(IEnumerable<ISaveable> saveables);
}

public interface ISerializer
{
    byte[] Serialize(object obj);
    T Deserialize<T>(byte[] data);
}


// Updated service interface for repository-like operations.
public interface IDataFileService : IEnumerable<ISaveData>
{
    // Save/Load/Delete for a specific slot
    Task SaveAt(int index);
    Task Load(int index);
    Task Delete(int index);

    // Bulk delete
    Task DeleteAll();

    // Create a new slot, returning its index
    Task<int> Create(string slotName, bool saveToDisk);

    // Capture data into a slot from provided saveables
    Task CaptureAt(int index, IEnumerable<ISaveable> saveables);

    // Clear any in-memory cache/state (used outside runtime for ScriptableObject assets)
    void ClearInMemory();
}

public class OdinSerializer : ISerializer
{
    [SerializeField] private DataFormat _dataFormat = DataFormat.Binary;

    public byte[] Serialize(object obj) => SerializationUtility.SerializeValue(obj, _dataFormat);

    public T Deserialize<T>(byte[] data) => SerializationUtility.DeserializeValue<T>(data, _dataFormat);
}

// Concrete save slot (unchanged)
[Serializable]
public sealed class SaveSlotData : ISaveData
{
    [FormerlySerializedAs("_saveName")]
    [SerializeField, LabelText("Name")] private string saveName;

    private DateTime _lastSaveTime;

    // Odin can serialize dictionaries and polymorphic values; keys are GUID strings.
    [OdinSerialize] private Dictionary<string, Dictionary<string, object>> _data = new();

    public string SaveName => saveName;
    public DateTime LastSaveTime => _lastSaveTime;
    public Dictionary<string, Dictionary<string, object>> DataBindings => _data;

    public SaveSlotData(string name)
    {
        saveName = name;
        _lastSaveTime = DateTime.MinValue;
    }

    public void CaptureData(IEnumerable<ISaveable> saveables)
    {
        if (saveables == null) return;
        _data.Clear();
        foreach (var s in saveables)
        {
            if (s == null || !s.SaveDataEnabled) continue;
            var guid = s.Guid;
            if (string.IsNullOrEmpty(guid)) continue;
            try
            {
                var state = s.CaptureState() ?? new Dictionary<string, object>();
                _data[guid] = state;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveSlot '{saveName}': Capture failed for asset {s.GetType().Name} ({guid}). Error: {ex}");
            }
        }
        _lastSaveTime = DateTime.Now;
    }

    public void ApplyData()
    {
        if (_data == null || _data.Count == 0) return;
        foreach (var kvp in _data)
        {
            var guid = kvp.Key;
            var state = kvp.Value;
            try
            {
                var asset = ScriptableObjectManagerAsset.Instance.GetAssetByGuid<ScriptableObject>(guid);
                if (asset is ISaveable saveable && saveable.SaveDataEnabled)
                {
                    saveable.RestoreState(state);
                }
                else if (asset == null)
                {
                    Debug.LogWarning($"SaveSlot '{saveName}': No asset found for GUID {guid}. Skipping.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveSlot '{saveName}': Apply failed for GUID {guid}. Error: {ex}");
            }
        }
    }
}

// Thin coordinator: tracks active slot index and discovered saveables, delegates work to the repository
[CreateAssetMenu(menuName = "Save/Save Data Manager")]
public class SaveDataSystem : SerializedScriptableObject
{
    [Title("Repository")]
    [InfoBox("Assign a repository (e.g., SaveRepository) that implements IDataFileService.")]
    [OdinSerialize] private IDataFileService _repository; // assign in inspector

    [Title("State"), InfoBox("Index of the slot used by actions below.")]
    [FormerlySerializedAs("_activeSlotIndex")]
    [SerializeField, MinValue(0)] private int activeSlotIndex;

    [Title("Saveables")] 
    [ShowInInspector, ReadOnly]
    private List<ISaveable> _foundSaveables = new();

    [ShowInInspector, ReadOnly]
    private IEnumerable<ISaveData> Slots => _repository != null ? _repository.ToList() : Enumerable.Empty<ISaveData>();

    private void OnEnable()
    {
        // Outside play mode, ensure repository in-memory data is cleared so the asset does not carry runtime state.
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            _repository?.ClearInMemory();
        }
#endif
        RefreshSaveables();
    }

    [Button(ButtonSizes.Medium)]
    public void RefreshSaveables()
    {
        _foundSaveables = ScriptableObjectManagerAsset.Instance
            .All()
            .OfType<ISaveable>()
            .Where(s => s.SaveDataEnabled)
            .ToList();
    }

    [Button(ButtonSizes.Medium)]
    public async Task CreateNewSave(string slotName)
    {
        if (_repository == null) { Debug.LogError("SaveDataSystem: Repository not assigned."); return; }
        var index = await _repository.Create(slotName, saveToDisk: false);
        activeSlotIndex = index;
        await _repository.CaptureAt(activeSlotIndex, _foundSaveables);
        await _repository.SaveAt(activeSlotIndex);
    }

    [Button]
    public async Task CaptureActive()
    {
        if (_repository == null) { Debug.LogError("SaveDataSystem: Repository not assigned."); return; }
        await _repository.CaptureAt(activeSlotIndex, _foundSaveables);
    }

    [Button]
    public async Task SaveActive()
    {
        if (_repository == null) { Debug.LogError("SaveDataSystem: Repository not assigned."); return; }
        await _repository.SaveAt(activeSlotIndex);
    }

    [Button]
    public async Task LoadActive()
    {
        if (_repository == null) { Debug.LogError("SaveDataSystem: Repository not assigned."); return; }
        await _repository.Load(activeSlotIndex);
    }

    [Button]
    public async Task DeleteActive()
    {
        if (_repository == null) { Debug.LogError("SaveDataSystem: Repository not assigned."); return; }
        await _repository.Delete(activeSlotIndex);
    }

    [Button(ButtonSizes.Medium), GUIColor(0.9f, 0.3f, 0.3f)]
    public async Task DeleteAll()
    {
        if (_repository == null) { Debug.LogError("SaveDataSystem: Repository not assigned."); return; }
        await _repository.DeleteAll();
    }

    [Button]
    public void ApplyActiveToAssets()
    {
        if (_repository == null) { Debug.LogError("SaveDataSystem: Repository not assigned."); return; }
        var slot = _repository.ElementAtOrDefault(activeSlotIndex);
        slot?.ApplyData();
    }
}

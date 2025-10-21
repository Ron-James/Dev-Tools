using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// Repository that manages save slots and persistence using injected serializer and storage services.
[Serializable]
public sealed class SaveRepository : IDataFileService
{
    [Title("Services")]
    [OdinSerialize] private ISerializer _serializer = new OdinSerializer();
    [OdinSerialize] private IDataStorage _storage = new PersistentDataStorage();

    [Title("Slots"), TableList(AlwaysExpanded = true)]
    [NonSerialized] private List<SaveSlotData> _slots = new();

    // Ensure list is always non-null (Odin may bypass ctor/field inits)
    private List<SaveSlotData> SlotsList => _slots ?? (_slots = new List<SaveSlotData>());

    [ShowInInspector, ReadOnly, LabelText("In-Memory Slots")]
    private IEnumerable<SaveSlotData> SlotsView => SlotsList;

    // Indexing helpers ------------------------------------------------------
    public int Count => SlotsList.Count;
    public ISaveData GetAt(int index) => (index >= 0 && index < SlotsList.Count) ? SlotsList[index] : null;

    private bool IsValid(int index) => index >= 0 && index < SlotsList.Count;

    private void EnsureReady() => _storage?.EnsureReady();

    // IEnumerable -----------------------------------------------------------
    public IEnumerator<ISaveData> GetEnumerator() => SlotsList.Cast<ISaveData>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Helper to save all (not part of interface)
    [Button, GUIColor(0.2f, 0.8f, 0.2f)]
    public async Task SaveAll()
    {
        EnsureReady();
        foreach (var slot in SlotsList)
        {
            await SaveInternal(slot);
        }
    }

    // IDataFileService implementation --------------------------------------
    public async Task SaveAt(int index)
    {
        if (!IsValid(index)) return;
        EnsureReady();
        await SaveInternal(SlotsList[index]);
    }

    public async Task Load(int index)
    {
        if (!IsValid(index)) return;
        EnsureReady();
        var name = SlotsList[index].SaveName;
        if (!_storage.Exists(name)) return;
        var bytes = await _storage.Read(name);
        var loaded = _serializer.Deserialize<SaveSlotData>(bytes);
        if (loaded != null)
        {
            SlotsList[index] = loaded;
        }
    }

    public async Task Delete(int index)
    {
        if (!IsValid(index)) return;
        EnsureReady();
        await _storage.Delete(SlotsList[index].SaveName);
    }

    public async Task DeleteAll()
    {
        EnsureReady();
        await _storage.DeleteAll(SlotsList.Select(s => s.SaveName));
    }

    public async Task<int> Create(string slotName, bool saveToDisk)
    {
        EnsureReady();
        if (string.IsNullOrWhiteSpace(slotName)) slotName = $"Save_{SlotsList.Count}";
        var slot = new SaveSlotData(slotName);
        SlotsList.Add(slot);
        var index = SlotsList.Count - 1;
        if (saveToDisk)
        {
            await SaveInternal(slot);
        }
        return index;
    }

    public Task CaptureAt(int index, IEnumerable<ISaveable> saveables)
    {
        if (!IsValid(index)) return Task.CompletedTask;
        SlotsList[index].CaptureData(saveables);
        return Task.CompletedTask;
    }

    public void ClearInMemory()
    {
        SlotsList.Clear();
    }

    [Button, GUIColor(0.6f, 0.8f, 1f)]
    public void RevealSaveFolder()
    {
        var root = _storage?.RootFolder;
        Debug.Log($"SaveRepository: Root save folder = {root}");
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(root))
        {
            UnityEditor.EditorUtility.RevealInFinder(root);
        }
#endif
    }

    // Internal --------------------------------------------------------------
    private async Task SaveInternal(SaveSlotData slot)
    {
        if (slot == null) return;
        if (_storage == null)
        {
            Debug.LogError("SaveRepository: Storage service is null; cannot write save file.");
            return;
        }
        if (_serializer == null)
        {
            _serializer = new OdinSerializer();
        }
        var path = _storage.GetPath(slot.SaveName);
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var bytes = _serializer.Serialize(slot);
            await _storage.Write(slot.SaveName, bytes);
            Debug.Log($"Saved slot '{slot.SaveName}' to: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveRepository: Failed writing '{path}'. Error: {ex}");
        }
    }
}

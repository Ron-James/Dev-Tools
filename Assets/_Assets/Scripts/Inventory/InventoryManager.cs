using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;


[ExecuteAlways]
[CreateAssetMenu(fileName = "InventoryManager", menuName = "Inventory/Inventory Manager", order = 0)]
public class InventoryManager : SerializableScriptableObject, IInventoryService, ISaveable, IEnumerable<IItemStack>
{
    [OdinSerialize] private IGuidAssetLookup _assetLookup;
    [Title("Inventory")] [OdinSerialize] private List<IItemStack> _itemEntries = new List<IItemStack>();


    [Title("UnityEvents")]
    [SerializeField] private UnityEvent<IItem> onInventoryChanged;
    [SerializeField] private UnityEvent<IItem> onItemAdded;
    
    private void OnEnable()
    {
        if (_assetLookup == null)
            _assetLookup = Resources.Load<ScriptableObjectManagerAsset>("DataManagers/ScriptableObjectManager");
        SetupItemAssets();
    }

    [Button("Setup Item Assets"), GUIColor("green")]
    private void SetupItemAssets()
    {
        IItem[] items = _assetLookup.AllOfType<IItem>().ToArray();
        foreach (var item in items)
        {
            AddItemWithoutNotify(item, 0);
        }
    }

    public int GetQuantity(IItem item)
    {
        IItemStack entry = _itemEntries.FirstOrDefault(e => e.Entry == item);
        return entry?.Quantity ?? 0;
    }

    public void AddItem(IItem item, int quantity)
    {
        AddItemWithoutNotify(item, quantity);
        item.OnItemAdded(quantity);
        onItemAdded?.Invoke(new ItemStack(item, quantity));
        IItemStack entry = _itemEntries.FirstOrDefault(e => e.Entry == item);
        onInventoryChanged?.Invoke(item);
    }


    public void AddItemWithoutNotify(IItem item, int quantity)
    {
        IItemStack entry = _itemEntries.FirstOrDefault(e => e.Entry == item);
        if (entry != null)
        {
            // Update existing entry
            int newQuantity = entry.Quantity + quantity;
            entry.Quantity = newQuantity;
        }
        else
        {
            // Add new entry
            ItemStack newEntry = new ItemStack(item, quantity);
            _itemEntries.Add(newEntry);
        }
    }

    #region ISa 
    
    public bool SaveDataEnabled { get; }

    public Dictionary<string, object> CaptureState()
    {
        Dictionary<string, object> state = new Dictionary<string, object>();
        state["inventoryEntries"] = _itemEntries;
        return state;
    }

    public void RestoreState(Dictionary<string, object> state)
    {
        SetupItemAssets();
        object entriesObj = state["inventoryEntries"];
        List<IItemStack> restoredEntries = entriesObj as List<IItemStack>;
        if (restoredEntries != null)
        {
            foreach (var entry in restoredEntries)
            {
                int quantity = entry.Quantity;
                IItem item = entry.Entry;
                AddItemWithoutNotify(item, quantity);
            }
        }
    }
    #endregion

    #region IEnumerable Implementation

    IEnumerator<IItemStack> IEnumerable<IItemStack>.GetEnumerator()
    {
        return _itemEntries.GetEnumerator();
    }

    public IEnumerator<IItem> GetEnumerator()
    {
        return _itemEntries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}
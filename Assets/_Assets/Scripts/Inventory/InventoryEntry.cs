using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class ItemStack : IItemStack
{
    [OdinSerialize] private IGuidReference<IItem> ItemReference = new GuidReference<IItem>();
    
    private int _quantity;

    public ItemStack(IItem item, int quantity)
    {
        ItemReference = new GuidReference<IItem> { Guid = item.Guid };
        Quantity = quantity;
    }
    
    public ItemStack()
    {
    }
    
    public void OnItemAdded(int quantity)
    {
        //Do something with the quantity change if needed
    }

    [ShowInInspector, ReadOnly]
    public int Quantity
    {
        get => _quantity;
        set
        {
            int temp = _quantity;
            int delta = value - temp;
            _quantity = value;
            OnItemAdded(delta);
        }
    }

    public IItem Entry
    {
        get => ItemReference.ResolvedAsset;
        set => ItemReference.Guid = value.Guid;
    }

    public bool Equals(IGuidAsset other)
    {
        return Entry.Guid == other.Guid;
    }

    public string Guid { get => Entry.Guid; }
    public void AssignGuid()
    {
        Entry.AssignGuid();
    }

    public string Name => Entry.Name;
    public Sprite Image => Entry.Image;
}
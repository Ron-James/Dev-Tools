using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

public abstract class Item : SerializableScriptableObject, IItem, IProduct
{
    public string Name { get; }
    public Sprite Image { get; }
    [Title("Unity Events")]
    [SerializeField] private UnityEvent<int> _onItemAdded;
    [SerializeField] private UnityEvent<int> _onItemRemoved;
    [SerializeField] private UnityEvent _onProductPurchased;
    public void OnItemAdded(int quantity)
    {
        if(quantity == 0) return;
        if (quantity > 0)
        {
            _onItemAdded?.Invoke(quantity);
        }
        else
        {
            _onItemRemoved?.Invoke(-quantity);
        }
    }

    [OdinSerialize] IItemStack[] _requiredItems => Array.Empty<IItemStack>();
    

    public IEnumerable<IItemStack> RequiredItems => _requiredItems;

    public IEnumerable<IItemStack> ProductItems => new[] { new ItemStack(this, 1) };
    
    
    

    public virtual void OnProductPurchased(IInventoryService inventoryService, IShop shop)
    {
        _onProductPurchased?.Invoke();
        inventoryService.AddItem(this, 1);
    }
    
    public void GetQuantity(InventoryManager inventoryManager)
    {
        inventoryManager.GetQuantity(this);
    }

    public bool Validate(IInventoryService item)
    {
        foreach (var requiredItem in RequiredItems)
        {
            if (item.GetQuantity(requiredItem.Entry) < requiredItem.Quantity)
            {
                return false;
            }
        }
        return true;
    }
}

public interface ICurrency : IItem
{
    
}

public abstract class Currency : Item, ICurrency
{
    
}


public interface ICustomComponent : IItem
{
    
}
public class CustomComponent : Item, ICustomComponent
{
    
}

public interface IExperience : IItem
{
    
}
public class Experience : Item, IExperience
{
    
}




public interface IConsumable : IItem
{
    void Consume(int quantity);
}
public abstract class Consumable : Item, IConsumable
{
    public abstract void Consume(int quantity);
}
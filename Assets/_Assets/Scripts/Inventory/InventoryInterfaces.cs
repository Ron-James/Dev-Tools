using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInventoryService : IEnumerable<IItem>
{
    int GetQuantity(IItem item);
    void AddItem(IItem item, int quantity);
}

public interface IEntity : IGuidAsset
{
    string Name { get; }
    Sprite Image { get; }
}
public interface IItem : IEntity 
{
    void OnItemAdded(int quantity);
}

public interface IQuantity
{
    int Quantity { get; set; }
}
public interface IValidator<in T>
{
    bool Validate(T item);
}
public interface IValidator<in T1, in T2>
{
    bool Validate(T1 item, T2 context);
}
public interface IShop : IEnumerable<IProduct>, IValidator<IProduct, IInventoryService>
{
    void Purchase(IProduct product, IInventoryService inventoryService);
}

public interface IProduct : IValidator<IInventoryService>
{
    IEnumerable<IItemStack> RequiredItems { get; }
    IEnumerable<IItemStack> ProductItems { get; }
    void OnProductPurchased(IInventoryService inventoryService, IShop shop);
}


public interface IEntry<T>
{
    T Entry { get; set; }
}
public interface IItemStack : IEntry<IItem>, IQuantity, IItem
{
    
}
public interface IProductStock : IEntry<IProduct>, IQuantity, IProduct
{
    bool IsInStock();

}
using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEngine;

public class ProductStock : IProductStock
{
    [OdinSerialize] private IProduct _entry;
    [SerializeField] int currentQuantity;
    [SerializeField] int maxQuantity;
    [SerializeField] bool infiniteStock;
    public IProduct Entry { get; set; }
    public int Quantity { get => currentQuantity; set => currentQuantity = value; }
    
    public bool IsInStock()
    {
        if(infiniteStock) return true;
        return Quantity > 0;
    }
    
    public IEnumerable<IItemStack> RequiredItems { get => _entry.RequiredItems; }
    public IEnumerable<IItemStack> ProductItems { get => _entry.ProductItems; }

    public void OnProductPurchased(IInventoryService inventoryService, IShop shop)
    {
        _entry.OnProductPurchased(inventoryService, shop);
    }

    public bool Validate(IInventoryService item)
    {
        foreach (var stack in RequiredItems)
        {
            if(item.GetQuantity(stack.Entry) < stack.Quantity)
            {
                return false;
            }
        }

        return true;
    }
}
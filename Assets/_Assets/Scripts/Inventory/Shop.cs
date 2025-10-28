using System.Collections.Generic;
using Sirenix.Serialization;

public class Shop : SerializableScriptableObject, IShop
{
    [OdinSerialize] private List<IProductStock> _products = new List<IProductStock>();
    [OdinSerialize] IInventoryService _inventoryService;

    public void Purchase(IProduct product, IInventoryService inventoryService)
    {
        if(Validate(product, inventoryService))
        {
            IProduct stock = _products.Find(p => p.Entry == product);
            stock.OnProductPurchased(inventoryService, this);
        }
    }

    public bool Validate(IProduct item, IInventoryService context)
    {
        IProductStock stock = _products.Find(p => p.Entry == item);
        if(stock != null)
        {
            return stock.IsInStock() && stock.Validate(context);
        }
        return false;
    }

    public IEnumerator<IProduct> GetEnumerator()
    {
        return _products.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return _products.GetEnumerator();
    }
}
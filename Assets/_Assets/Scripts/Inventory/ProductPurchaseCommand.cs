using System.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

public class ProductPurchaseCommand : ICommand<IProduct>
{
    [OdinSerialize] private IProduct _defaultProduct;
    [OdinSerialize]private IShop _shop;
    [OdinSerialize] private IInventoryService _inventoryService;
    [SerializeField] private UnityEvent<IProduct> onProductPurchaseRejected;

    public ProductPurchaseCommand(IShop shop, IInventoryService inventoryService)
    {
        _shop = shop;
        _inventoryService = inventoryService;
    }
    
    public ProductPurchaseCommand()
    {
        
    }

    public Task Execute(IProduct parameter)
    {
        if (!_shop.Validate(parameter, _inventoryService))
        {
            onProductPurchaseRejected?.Invoke(parameter);
            return Task.CompletedTask;
        }
        _shop.Purchase(parameter, _inventoryService);
        return Task.CompletedTask;
    }
    

    public Task Execute()
    {
        _shop.Purchase(_defaultProduct, _inventoryService);
        return Task.CompletedTask;
    }

    // Undo: attempt to refund/remove the purchased product if possible. Default no-op when services are unavailable.
    public Task Undo()
    {
        return Task.CompletedTask;
    }
}


public class ProductCommandExecutor : CommandExecutorListener<IProduct>
{
    
}
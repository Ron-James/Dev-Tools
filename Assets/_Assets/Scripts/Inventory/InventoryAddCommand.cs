using System.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

public class InventoryAddCommand : ICommand<int>
{
    [OdinSerialize] private IInventoryService _inventoryService;
    [OdinSerialize] private IItem _item;
    [SerializeField] private int _quantity;
    
    
    public InventoryAddCommand()
    {
        _inventoryService = null;
        _item = null;
        _quantity = 0;
    }

    public InventoryAddCommand(IInventoryService inventoryService, IItem item, int quantity)
    {
        _inventoryService = inventoryService;
        _item = item;
        _quantity = quantity;
    }

    public Task Execute(int parameter)
    {
        _inventoryService.AddItem(_item, parameter);
        return Task.CompletedTask;
    }

    public Task Execute()
    {
        _inventoryService.AddItem(_item, _quantity);
        return Task.CompletedTask;
    }

    // Undo: remove the previously added quantity. Best-effort; swallow exceptions.
    public Task Undo()
    {
        // Default no-op undo. If your inventory service supports a removal API,
        // implement a specialized undo command or override this method.
        return Task.CompletedTask;
    }
}

using System;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

public class ItemEventBusListener : EventBusListener<IItem>
{
    [OdinSerialize] IInventoryService inventoryService;
    [SerializeField] private UnityEvent<int> _onQuantity;
    [SerializeField] private UnityEvent<int> onInventoryQuantity;
    [SerializeField] protected StringEventProcessor onItemName = new StringEventProcessor();
    [SerializeField] protected UnityEvent<Sprite> onItemImage = new UnityEvent<Sprite>();
    protected override void TriggerEvents(IItem eventData)
    {
        if(eventData is IQuantity quantityItem)
        {
            _onQuantity?.Invoke(quantityItem.Quantity);
        }
        onItemName.Process(eventData.Name);
        onItemImage?.Invoke(eventData.Image);
        onInventoryQuantity?.Invoke(inventoryService.GetQuantity(eventData));
    }

    protected override void TriggerEvents()
    {
        
    }


    private void OnValidate()
    {
        if(inventoryService == null)
        {
            inventoryService = Resources.Load<InventoryManager>("DataManagers/MainInventory");
        }
    }
    
}
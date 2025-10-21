using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

public abstract class EventBusListener<T> : SerializedMonoBehaviour, IEventListener<T>
{
    [Title("General Settings")] [SerializeField]
    private bool CheckValueAssetsOnStart;
    [Title("Event Bus")]
    [OdinSerialize] protected IEventBus[] eventBuses;
    [Title("Filter Settings")]
    [SerializeField] private bool useFilter = false;
    [ShowIf(nameof(useFilter)), SerializeField] private T listenValue;
    [SerializeField, ShowIf(nameof(useFilter))] private bool inverseFilter = false;
    [SerializeField, ShowIf(nameof(useFilter))] private List<T> filterValues = new List<T>();
    
    [Title("Unity Events")]
    [SerializeField] private UnityEvent<T> onEventRaised;
    [SerializeField] private UnityEvent onEventRaisedEmpty;
    
    [Button]
    public void OnEventRaised(T eventData)
    {
        Debug.Log($"EventBusListener received event data: {eventData}");
        if (!FilterAllows(eventData))
        {
            return;
        }
        onEventRaised?.Invoke(eventData);
        onEventRaisedEmpty?.Invoke();
        TriggerEvents(eventData);
    }
    
    
    public void TriggerListenValue()
    {
        try
        {
            OnEventRaised(listenValue);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error triggering listen value: {e.Message}");
        }
        
    }


    protected abstract void TriggerEvents(T eventData);
    protected abstract void TriggerEvents();
    
    public void OnEventRaised()
    {
        onEventRaisedEmpty?.Invoke();
        TriggerEvents();
    }

    protected void OnEnable()
    {
        SubscribeToBuses();
    }
    protected void OnDisable()
    {
        UnsubscribeFromBuses();
    }

    protected void Start()
    {
        if (CheckValueAssetsOnStart && eventBuses != null)
        {
            foreach (var bus in eventBuses)
            {
                if (bus is IValueAsset<T> valueAssetBus)
                {
                    OnEventRaised(valueAssetBus.StoredValue);
                }
            }
        }
    }

    protected void SubscribeToBuses()
    {
        if (eventBuses == null) return;
        foreach (var bus in eventBuses)
        {
            if (bus is IEventBus<T> typedBus)
            {
                typedBus.Subscribe(this);
            }
            else
            {
                bus.Subscribe(this);
            }
        }
    }
    
    protected void UnsubscribeFromBuses()
    {
        if (eventBuses == null) return;
        foreach (var bus in eventBuses)
        {
            if (bus is IEventBus<T> typedBus)
            {
                typedBus.Unsubscribe(this);
            }
            else
            {
                bus.Unsubscribe(this);
            }
        }
    }
    
    
    
    [Button]
    protected virtual bool FilterAllows(T eventData)
    {
        if (!useFilter) return true;
        bool isListening = EqualityComparer<T>.Default.Equals(eventData, listenValue);
        bool inFilterList = filterValues.Contains(eventData);
        bool result = isListening || inFilterList;
        if (inverseFilter) result = !result;
        return result;
    }
}
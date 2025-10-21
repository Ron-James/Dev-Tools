using System; // added for Exception
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public abstract class EventBusSO<T> : SerializableScriptableObject, IEventBus<T>, IValueAsset<T>
{

    [Title("Listeners")]
    [OdinSerialize] private List<IEventListener> _listeners = new();
    [Title("Raise Value")]
    [OdinSerialize] protected T raiseValue;
    [Title("Unity Events")]
    [SerializeField] protected UnityEvent<T> onRaised;
    
    [Title("Stored Data")]
    private T _storedData;
    public IEnumerator<IEventListener> GetEnumerator()
    {
        return _listeners.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    [Button, GUIColor("green")]
    public void Raise()
    {
        Raise(raiseValue);
    }

    [Button, GUIColor("green")]
    public void Raise(T eventData)
    {
        // Iterate backwards over a snapshot to avoid issues if listeners modify the list
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            var listener = _listeners[i];
            try
            {
                if (listener is IEventListener<T> messageListener)
                {
                    messageListener.OnEventRaised(eventData);
                }
                else
                {
                    listener.OnEventRaised();
                }
            }
            catch (Exception ex)
            {
                var busName = name;
                string listenerInfo = listener is UnityEngine.Object uo
                    ? $"{uo.GetType().Name} '{uo.name}'"
                    : listener?.GetType().FullName ?? "(null)";
                Debug.LogError($"[EventBus:{busName}] Listener {listenerInfo} threw: {ex.Message}", this);
                Debug.LogException(ex, this);
            }
        }
        _storedData = eventData;
        try
        {
            onRaised?.Invoke(eventData);
        }
        catch (Exception ex)
        {
            var busName = name;
            Debug.LogError($"[EventBus:{busName}] UnityEvent onRaised threw: {ex.Message}", this);
            Debug.LogException(ex, this);
        }
        
    }

    [Button, GUIColor("green")]
    public void Raise(UnityEngine.Object obj){
        
        if (obj.GetComponent<T>() != null)
        {
            Raise(obj.GetComponent<T>());    
        }
        else if (obj is T tObj)
        {
            Raise(tObj);
        }
        else if (obj is IValueAsset<T> valueAsset && valueAsset.StoredValue != null)
        {
            Raise(valueAsset.StoredValue);
        }
    }
    [Button, GUIColor("green")]
    public void Subscribe(IEventListener listener)
    {
        _listeners.Add(listener);
    }
    
    [Button, GUIColor("green")]
    public void Unsubscribe(IEventListener listener)
    {
        if (_listeners.Contains(listener))
        {
            _listeners.Remove(listener);
        }
    }

    [Button]
    public void RemoveAllListeners()
    {
        _listeners.Clear();
    }

    [ShowInInspector, ReadOnly]
    public T StoredValue { get => _storedData; set => Raise(value); }

}
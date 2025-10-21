using System; // for Exception
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Entities;
using UnityEngine;


[CreateAssetMenu (fileName = "VoidEventBus", menuName = "Event Busses/Void Event Bus", order = 0)]
public class VoidEventBusSO : SerializableScriptableObject, IEventBus
{
    
    [OdinSerialize] private List<IEventListener> _listeners = new();
    public IEnumerator<IEventListener> GetEnumerator()
    {
        return _listeners.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            var listener = _listeners[i];
            try
            {
                listener.OnEventRaised();
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
    }

    public void Subscribe(IEventListener listener)
    {
        _listeners.Add(listener);
    }

    public void Unsubscribe(IEventListener listener)
    {
        if (_listeners.Contains(listener))
        {
            _listeners.Remove(listener);
        }
    }

    public void RemoveAllListeners()
    {
        _listeners.Clear();
    }
}
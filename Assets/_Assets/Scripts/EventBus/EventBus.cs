using System.Collections;
using System.Collections.Generic;
using Sirenix.Serialization;

public class EventBus : IEventBus
{
    
    [OdinSerialize] protected List<IEventListener> _listeners = new List<IEventListener>();
    public IEnumerator<IEventListener> GetEnumerator()
    {
        return _listeners.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public virtual void Raise()
    {
        for(int loop = _listeners.Count - 1; loop >= 0; loop--)
        {
            var listener = _listeners[loop];
            listener.OnEventRaised();
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

public class EventBus<T1, T2> : EventBus, IEventBus<T1, T2>
{
    [OdinSerialize] protected T1 _defaultRaiseValue1;
    [OdinSerialize] protected T2 _defaultRaiseValue2; 
    
    public override void Raise()
    {
        Raise(_defaultRaiseValue1, _defaultRaiseValue2);
    }

    public virtual void Raise(T1 eventData1, T2 eventData2)
    {
        for(int loop = _listeners.Count - 1; loop >= 0; loop--)
        {
            var listener = _listeners[loop];
            if(listener is IEventListener<T1, T2> typedListener)
            {
                typedListener.OnEventRaised(eventData1, eventData2);
            }
            if(listener is IEventListener<T1> typedListener1)
            {
                typedListener1.OnEventRaised(eventData1);
            }
            if(listener is IEventListener<T2> typedListener2)
            {
                typedListener2.OnEventRaised(eventData2);
            }
        }
    }
}



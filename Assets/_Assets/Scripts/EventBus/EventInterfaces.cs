using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.Serialization;


public interface IEventBus : IEnumerable<IEventListener>
{
    void Raise();
    void Subscribe(IEventListener listener);
    void Unsubscribe(IEventListener listener);
    void RemoveAllListeners();

}
public interface IEventBus<in T> : IEventBus
{
    void Raise(T eventData);
}


public interface IEventBus<in T1, in T2> : IEventBus
{
    void Raise(T1 eventData1, T2 eventData2);
}

public interface IEventListener<in T1, in T2> :  IEventListener
{
    void OnEventRaised(T1 eventData1, T2 eventData2);
}



public interface IEventListener<in T> :  IEventListener
{
    void OnEventRaised(T eventData);
}


public interface IEventListener
{
    void OnEventRaised();
}


public interface IValueAsset<T>
{
    T StoredValue { get; set; }
}


public class EventBus<T> : EventBus, IEventBus<T>
{
    [OdinSerialize] protected T _defaultRaiseValue;
    public override void Raise()
    {
        Raise(_defaultRaiseValue);
    }

    public virtual void Raise(T eventData)
    {
        for(int loop = _listeners.Count - 1; loop >= 0; loop--)
        {
            var listener = _listeners[loop];
            if(listener is IEventListener<T> typedListener)
            {
                typedListener.OnEventRaised(eventData);
            }
        }
        _defaultRaiseValue = eventData;
    }
}


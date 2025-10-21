using System.Collections.Generic;
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



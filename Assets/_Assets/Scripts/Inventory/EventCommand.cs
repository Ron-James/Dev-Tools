using System.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

public class EventCommand : ICommand
{
    [SerializeField] UnityEvent _event;
    [OdinSerialize] IEventBus _eventBus;
    
    public EventCommand()
    {
        _event = null;
        _eventBus = null;
    }


    public Task Execute()
    {
        _event?.Invoke();
        _eventBus?.Raise();
        return Task.CompletedTask;
    }

    public Task Undo()
    {
        // EventCommand doesn't change state by default; undo is a no-op.
        return Task.CompletedTask;
    }
}

public class EventCommand<T> : EventCommand, ICommand<T>
{
    [OdinSerialize] private IEventBus<T> _eventBus;
    [SerializeField] private UnityEvent<T> _event;
    
    public EventCommand()
    {
        _eventBus = null;
        _event = null;
    }

    public Task Execute(T parameter)
    {
        _event?.Invoke(parameter);
        _eventBus?.Raise(parameter);
        return Task.CompletedTask;
    }
}
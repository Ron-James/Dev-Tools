using Sirenix.Serialization;

public class CommandListener : IEventListener
{
    [OdinSerialize] protected ICommand _command;
    public void OnEventRaised()
    {
        _ = _command.Execute();
    }
}

public class CommandListener<T> : IEventListener<T>
{
    [OdinSerialize] protected ICommand<T> _command;
    [OdinSerialize] protected T _defaultParameter;
    public void OnEventRaised(T eventData)
    {
        _ = _command.Execute(eventData);
    }

    public void OnEventRaised()
    {
        _ = _command.Execute(_defaultParameter);
    }
}
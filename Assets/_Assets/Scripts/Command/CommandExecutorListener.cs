using System.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;

public class CommandExecutorListener : IEventListener
{
    [OdinSerialize] private ICommand[] _commandsToExecute;
    [SerializeField] private bool _executeInParallel = false;
    
    public void OnEventRaised()
    {
        _ = ExecuteCommands();
    }
    
    protected async Task ExecuteCommands()
    {
        if (_executeInParallel)
        {
            Task[] tasks = new Task[_commandsToExecute.Length];
            for (int i = 0; i < _commandsToExecute.Length; i++)
            {
                tasks[i] = _commandsToExecute[i].Execute();
            }
            await Task.WhenAll(tasks);
        }
        else
        {
            foreach (var command in _commandsToExecute)
            {
                await command.Execute();
            }
        }
    }
}


public class CommandExecutorListener<T> : CommandExecutorListener, IEventListener<T>
{
    [OdinSerialize] private ICommand<T>[] _commandsToExecute;
    [SerializeField] private bool _executeInParallel = false;
    
    public void OnEventRaised(T eventData)
    {
        _ = ExecuteCommands(eventData);
    }
    
    protected async Task ExecuteCommands(T parameter)
    {
        if (_executeInParallel)
        {
            Task[] tasks = new Task[_commandsToExecute.Length];
            for (int i = 0; i < _commandsToExecute.Length; i++)
            {
                tasks[i] = _commandsToExecute[i].Execute(parameter);
            }
            await Task.WhenAll(tasks);
        }
        else
        {
            foreach (var command in _commandsToExecute)
            {
                await command.Execute(parameter);
            }
        }
    }
}
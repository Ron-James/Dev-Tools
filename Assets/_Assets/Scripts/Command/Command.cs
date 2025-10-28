using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public abstract class Command : ICommand
{
    [SerializeField] protected UnityEvent onCommandStarted;
    [SerializeField] protected UnityEvent onCommandCompleted;
    public abstract Task Execute();
    
    // Default no-op Undo. Concrete commands should override with meaningful behavior.
    public virtual Task Undo() => Task.CompletedTask;
}
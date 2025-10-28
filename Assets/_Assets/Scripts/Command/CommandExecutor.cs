using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

// --------------------------------------------------------------------------------------
// Command Pattern: Minimal async contract
// --------------------------------------------------------------------------------------
// A command encapsulates one unit of work that can be executed by the executor.
// - Execute is awaited so commands can span multiple frames without blocking the main thread.
// - No cancellation token is used here to keep it simple; the executor can only stop
//   between commands (documented below).
public interface ICommand
{
    Task Execute();
    // Every command must provide an Undo operation. For simple commands that do not
    // change state or where undo isn't meaningful, implementers should provide a no-op
    // Task.CompletedTask implementation. This strict contract keeps the invoker simple
    // and consistent: every completed command can be undone.
    Task Undo();
}




// --------------------------------------------------------------------------------------
// Example Command: Play an AudioClip and wait until it finishes
// --------------------------------------------------------------------------------------
// Demonstrates a command that yields frame-by-frame using Awaitable.NextFrameAsync
// (Unity main thread). It never leaves the main thread and does not use Task.Run.
[Serializable]
public class AudioPlayCommand : ICommand
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private UnityEvent onCommandComplete;

    public async Task Execute()
    {
        // Basic validation so we don't crash during execution.
        if (audioSource == null || audioClip == null)
        {
            Debug.LogWarning("AudioPlayCommand skipped: Missing AudioSource or AudioClip.");
            return;
        }

        audioSource.PlayOneShot(audioClip);
        // Wait while clip is playing.
        await Awaitable.WaitForSecondsAsync(audioClip.length);

        onCommandComplete?.Invoke();
    }
    
    // Playing audio is not stateful for undo in this sample; provide no-op.
    public Task Undo() => Task.CompletedTask;
}



public class CompositeCommand : ICommand
{
    [OdinSerialize,
     ListDrawerSettings(ShowPaging = true, DraggableItems = true, ShowFoldout = true, DefaultExpandedState = true)]
    private List<ICommand> _subCommands = new List<ICommand>();

    public async Task Execute()
    {
        if (_subCommands == null || _subCommands.Count == 0)
            return;
        // Start all sub-commands.
        List<Task> commandTasks = new List<Task>();
        foreach (var cmd in _subCommands)
        {
            commandTasks.Add(cmd.Execute());
        }

        // Await all to complete.
        await Task.WhenAll(commandTasks);
    }
    
    // CompositeUndo: undo subcommands in reverse order
    public async Task Undo()
    {
        if (_subCommands == null || _subCommands.Count == 0) return;
        for (int i = _subCommands.Count - 1; i >= 0; i--)
        {
            try { await _subCommands[i].Undo(); }
            catch (Exception ex) { Debug.LogError($"CompositeCommand.Undo: {ex}"); }
            await Awaitable.NextFrameAsync();
        }
    }
}

// --------------------------------------------------------------------------------------
// Example Command: Wait for an external event a number of times
// --------------------------------------------------------------------------------------
// This shows how a command can rely on some external event bus and complete only after
// receiving a certain number of notifications.
public class MessageWaitCommand : ICommand, IEventListener
{
    [OdinSerialize] private IEventBus _eventBus; // Provided via inspector (Odin-serialized interface)
    [SerializeField] private int _requiredCallCount = 1; // How many event hits required
    [ShowInInspector, ReadOnly] private int _callCount; // Runtime counter for received events
    private UnityEvent _onCommandComplete;

    public async Task Execute()
    {
        // Reset state for safety if this command is reused.
        _callCount = 0;

        if (_eventBus == null)
        {
            Debug.LogWarning("MessageWaitCommand skipped: eventBus not set.");
            return;
        }

        SubscribeEvent();
        // Yield until the required number of events have been received.
        while (_callCount < _requiredCallCount)
        {
            await Awaitable.NextFrameAsync();
        }

        // Always unsubscribe, even if an exception happens inside the loop.
        UnsubscribeEvent();


        _onCommandComplete?.Invoke();
    }

    // MessageWaitCommand doesn't mutate state by default; Undo is no-op here.
    public Task Undo() => Task.CompletedTask;

    private void SubscribeEvent() => _eventBus.Subscribe(this);
    private void UnsubscribeEvent() => _eventBus.Unsubscribe(this);

    // Called by the event bus when the relevant event is raised.
    public void OnEventRaised() => _callCount++;
}

// --------------------------------------------------------------------------------------
// CommandExecutor: Sequential command runner
// --------------------------------------------------------------------------------------
// Responsibilities
// - Holds a queue of commands (ICommand) and runs them sequentially.
// - Provides simple Start/Stop/Pause controls from the inspector via Odin buttons.
// - Emits UnityEvents for queue lifecycle so designers can hook into it if needed.
// - Never uses Task.Run; all awaits are frame-based to stay on the main thread.
// Limitations
// - Stop cancels between commands only (no per-command cancellation support here).
// - Pause only affects the gap between commands (does not pause an in-flight command).
public partial class CommandExecutor : SerializedMonoBehaviour, ICommandInvoker
{
    // Queue of commands to execute (Inspector-visible thanks to Odin).
    [OdinSerialize,
     ListDrawerSettings(ShowPaging = true, DraggableItems = true, ShowFoldout = true, DefaultExpandedState = true)]
    private Queue<ICommand> _commandQueue = new Queue<ICommand>();

    // Completed commands history (optional, useful for debugging/retry UI).
    [OdinSerialize, ShowInInspector, ReadOnly]
    private List<ICommand> _completedCommands = new List<ICommand>();

    // Lifecycle toggles and status (for convenience in the inspector).
    [SerializeField] private bool autoRunOnStart; // Start automatically on Start() if true

    [ShowInInspector, ReadOnly] public bool IsRunning { get; private set; }

    // Reference to the current command being executed
    [ShowInInspector, ReadOnly] private ICommand _currentCommand;

    // ----------------------------------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------------------------------
    private void Start()
    {
        if (autoRunOnStart && !IsRunning)
        {
            _ = RunQueueAsync(); // Fire-and-forget; we keep it on the main thread via Awaitable
        }
    }

    // ----------------------------------------------------------------------------------
    // Public API: Manage queue items
    // ----------------------------------------------------------------------------------

    // Enqueue a command at the end of the queue.
    [Button]
    public void Enqueue(ICommand command)
    {
        if (command == null) return;
        _commandQueue.Enqueue(command);
    }

    // Enqueue multiple commands.
    [Button]
    public void EnqueueRange(IEnumerable<ICommand> commands)
    {
        if (commands == null) return;
        foreach (var command in commands)
        {
            if (command != null) _commandQueue.Enqueue(command);
        }
    }

    // ----------------------------------------------------------------------------------
    // Inspector Buttons (Odin)
    // ----------------------------------------------------------------------------------

    [Button(ButtonSizes.Medium)]
    public async void StartQueue()
    {
        if (IsRunning) return;
        await RunQueueAsync();
    }

    // Execute only the next command in the queue (no queue lifecycle events fired).
    [Button(ButtonSizes.Medium)]
    public async void NextCommand()
    {
        if (IsRunning) return;
        await RunNextCommandAsync();
    }

    private async Task RunNextCommandAsync()
    {
        if (IsRunning) return;
        if (_commandQueue.Count == 0) return;

        IsRunning = true;

        var cmd = _commandQueue.Dequeue();
        _currentCommand = cmd;
        if (cmd == null) return;

        await cmd.Execute();


        _completedCommands.Add(cmd);
        await Awaitable.NextFrameAsync();
        _currentCommand = null;
        IsRunning = false;
    }

    // ----------------------------------------------------------------------------------
    // Core runner loop
    // ----------------------------------------------------------------------------------
    private async Task RunQueueAsync()
    {
        if (IsRunning) return;

        IsRunning = true;

        try
        {
            // Drain the queue sequentially.
            while (_commandQueue.Count > 0)
            {
                var cmd = _commandQueue.Dequeue();
                _currentCommand = cmd;
                if (cmd == null)
                {
                    // Skip nulls safely; continue with next.
                    continue;
                }

                try
                {
                    await cmd.Execute();
                }
                catch (Exception ex)
                {
                    // Fail fast but keep the executor alive so later commands can still run.
                    Debug.LogError($"CommandExecutor: Command threw exception: {ex}");
                }

                _completedCommands.Add(cmd);
                // Let the frame breathe between commands to keep UI responsive.
                await Awaitable.NextFrameAsync();
            }
        }
        finally
        {
            _currentCommand = null;
            IsRunning = false;
        }
    }

    public Task InvokeCommand(ICommand command)
    {
        Enqueue(command);
        return RunQueueAsync();
    }

    public Task InvokeQueuedCommands(IEnumerable<ICommand> commands)
    {
        EnqueueRange(commands);
        return RunQueueAsync();
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

public interface ICommandInvoker
{
    Task InvokeCommand(ICommand command);
    Task InvokeQueuedCommands(IEnumerable<ICommand> commands);
}


public interface ICommandQueue : ICommandInvoker
{
    /// <summary>
    /// Enqueue a command to be executed in FIFO order. Returns a task that completes when the command finishes (or faults).
    /// </summary>
    Task QueueCommand(ICommand command);

    /// <summary>Undo the last completed command (LIFO).</summary>
    Task UndoLastAsync();

    /// <summary>Undo all recorded commands (LIFO).</summary>
    Task UndoAllAsync();

    /// <summary>Number of entries currently recorded in undo history.</summary>
    int UndoHistoryCount { get; }

    /// <summary>Whether there is any undoable history available.</summary>
    bool HasUndoableHistory { get; }

    /// <summary>Number of items currently queued (not including the running one).</summary>
    int QueueCount { get; }

    /// <summary>Whether the invoker is currently processing the queue.</summary>
    bool IsProcessing { get; }
}


// Enhanced, reusable CommandInvoker
// - Tracks the currently executing command
// - Keeps a history stack of completed ICommand instances (LIFO undo)
// - Safe: uses ConcurrentQueue + Interlocked to avoid an explicit queue lock while keeping undo history lock-free
/// <summary>
/// Concrete command invoker with a persistent FIFO queue, undo history, and lifecycle helpers.
/// Designed to be used on the Unity main thread; awaits yield with Awaitable.NextFrameAsync to keep UI responsive.
/// Cancellation of the processing pipeline has been removed for now; the invoker runs queued commands until the queue is empty.
/// </summary>
public class CommandInvoker : ICommandQueue
{
    // Undo history for successfully completed commands (LIFO). Main-thread only: plain Stack is sufficient.
    private readonly Stack<ICommand> _undoHistory = new Stack<ICommand>();

    // Persistent processing queue (main-thread only)
    private readonly Queue<(ICommand Command, TaskCompletionSource<object> Completion)> _queue;

    // Processing state (main-thread usage assumed)
    private bool _isProcessing;

    // Currently executing command (may be null)
    private ICommand _currentCommand;

    // Public read-only accessor for status inspection (Odin shows this field)
    [ShowInInspector, ReadOnly]
    public ICommand CurrentCommand => _currentCommand;
    

    // Constructor to initialize readonly collections
    public CommandInvoker()
    {
        _queue = new Queue<(ICommand, TaskCompletionSource<object>)>();
    }

    // Ensure the background processor is started. This is lock-free and atomic.
    private void StartProcessingIfNeeded()
    {
        // If not already processing, mark processing and start the loop.
        if (!_isProcessing)
        {
            _isProcessing = true;
            _ = ProcessQueueAsync();
        }
    }

    // Public API: enqueue and start processing if necessary
    public Task InvokeCommand(ICommand command) => QueueCommand(command);

    public Task QueueCommand(ICommand command)
    {
        if (command == null)
        {
            Debug.LogWarning("CommandInvoker: QueueCommand called with null command.");
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Enqueue((command, tcs));
        StartProcessingIfNeeded();
        return tcs.Task;
    }

    // Centralized helper to record undoable commands safely
    private void RecordUndoableIfNeeded(ICommand cmd)
    {
        // Main-thread only: simple push to the Stack.
        if (cmd != null)
        {
            _undoHistory.Push(cmd);
        }
    }

    // Enqueue multiple commands atomically (each gets its own TCS) and start processing.
    public Task InvokeQueuedCommands(IEnumerable<ICommand> commands)
    {
        if (commands == null) return Task.CompletedTask;

        var tcsList = new List<TaskCompletionSource<object>>();

        foreach (var cmd in commands)
        {
            if (cmd == null) continue;
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue((cmd, tcs));
            tcsList.Add(tcs);
        }

        StartProcessingIfNeeded();

        var tasks = new List<Task>(tcsList.Count);
        foreach (var t in tcsList) tasks.Add(t.Task);
        return Task.WhenAll(tasks);
    }

    // Internal processing loop — dequeues items and runs them sequentially.
    private async Task ProcessQueueAsync()
    {
        try
        {
            while (true)
            {
                // Try to dequeue an item; if none, attempt to clear the processing state and exit safely.
                if (_queue.Count == 0)
                {
                    // No items available right now: clear the processing state so future enqueues can restart processing.
                    _isProcessing = false;

                    // If items were enqueued after we observed empty, try to re-acquire processing and continue.
                    if (_queue.Count > 0 && !_isProcessing)
                    {
                        _isProcessing = true;
                        continue;
                    }

                    // Nothing to do — stop processing.
                    return;
                }

                var item = _queue.Dequeue();
                var cmd = item.Command;
                var completion = item.Completion;

                if (cmd == null)
                {
                    completion?.TrySetResult(null);
                    continue;
                }

                _currentCommand = cmd;

                try
                {
                    await cmd.Execute();

                    // Record this completed command in the undo history. Every ICommand has Undo().
                    RecordUndoableIfNeeded(cmd);

                    completion?.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"CommandInvoker: Execute threw exception in queued processor: {ex}");
                    completion?.TrySetException(ex);
                }
                finally
                {
                    _currentCommand = null;
                }

                // yield a frame between queue items to keep UI responsive
                await Awaitable.NextFrameAsync();
            }
        }
        finally
        {
            // Ensure processing flag cleared on any unexpected exit
            _isProcessing = false;
        }
    }

    // Public status properties
    /// <summary>Number of items currently queued (not including the running one).</summary>
    public int QueueCount => _queue.Count;

    /// <summary>Whether the invoker is currently processing the queue.</summary>
    public bool IsProcessing => _isProcessing;

    // Capability properties
    public bool HasUndoableHistory => UndoHistoryCount > 0;

    // Undo the last completed command (LIFO). Safe: logs exceptions instead of throwing.
    public async Task UndoLastAsync()
    {
        if (_undoHistory.Count == 0)
        {
            Debug.Log("CommandInvoker: No undoable commands in history.");
            return;
        }

        var cmd = _undoHistory.Pop();

        try
        {
            await cmd.Undo();
        }
        catch (Exception ex)
        {
            Debug.LogError($"CommandInvoker: Undo threw exception: {ex}");
        }
    }

    // Undo all recorded commands in reverse order (LIFO). Continues on errors and logs them.
    public async Task UndoAllAsync()
    {
        while (_undoHistory.Count > 0)
        {
            var cmd = _undoHistory.Pop();

            try
            {
                await cmd.Undo();
            }
            catch (Exception ex)
            {
                Debug.LogError($"CommandInvoker: UndoAll - individual Undo threw exception: {ex}");
            }

            // Yield a frame so long-running undos don't freeze the main thread if they use frame-based awaits.
            await Awaitable.NextFrameAsync();
        }
    }

    // Optionally expose the undo history count (read-only)
    public int UndoHistoryCount => _undoHistory.Count;
}

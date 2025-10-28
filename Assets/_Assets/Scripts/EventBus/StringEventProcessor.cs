using UnityEngine.Events;

public struct StringEventProcessor 
{
    public string prefix;
    public string suffix;
    public UnityEvent<string> onProcessed;
    public void Process(string eventData)
    {
        onProcessed?.Invoke($"{prefix}{eventData}{suffix}");
    }
}
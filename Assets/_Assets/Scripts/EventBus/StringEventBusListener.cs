public class StringEventBusListener : EventBusListener<string>
{
    protected override void TriggerEvents(string eventData)
    {
        // Implement any additional logic needed when the event is raised with data
    }

    protected override void TriggerEvents()
    {
        // Implement any additional logic needed when the event is raised without data
    }
}
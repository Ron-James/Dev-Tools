using OdinEvents;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public interface IEntry
{
    void PrintEntry()
    {
    Debug.Log("IEntry PrintEntry");
    }
}
public class EventTester : SerializedMonoBehaviour
{
   [OdinSerialize] private OdinUnityEvent<IEntry> entryEvent;

   [Button]
   public void TestInvoke()
   {
       EntryImpl entry = new EntryImpl("TestEntry");
       entryEvent.Invoke(entry);
   }
   
   
   
   public void OnEntryRaised(IEntry entry)
   {
       Debug.Log("OnEntryRaised called");
       entry.PrintEntry();
   }
   
   
   
   
}
public class EntryImpl : IEntry
{
    [SerializeField]
    private string _name;
    
    public EntryImpl()
    {
        _name = "DefaultEntry";
    }
    public EntryImpl(string name)
    {
        _name = name;
    }
    public void PrintEntry()
    {
        Debug.Log($"EntryImpl {_name} PrintEntry");
    }
}

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class ScriptableObjectLoadDebug : SerializedMonoBehaviour
{
    [FormerlySerializedAs("_scriptableObjects")]
    [SerializeField] private ScriptableObject[] scriptableObjects;

    [SerializeReference] private IGuidAssetLookup _lookup;

    private IGuidAssetLookup Lookup => _lookup ?? ScriptableObjectManagerAsset.Instance;

    private void Start()
    {
        UpdateList();
    }

    [Button]
    public void UpdateList()
    {
        scriptableObjects = Lookup.All();
    }
    
    [Button]
    public ScriptableObject GetObjectByGuid(string guid)
    {
        return Lookup.GetAssetByGuid<ScriptableObject>(guid);
    }
}
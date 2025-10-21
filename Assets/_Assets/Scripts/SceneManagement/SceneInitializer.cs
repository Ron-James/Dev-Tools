using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OdinEvents;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;


public interface IInitializable
{
    Task Init();
}

[DefaultExecutionOrder(10000)]
public class SceneInitializer : SerializedMonoBehaviour
{
    [SerializeField] private GameObject _loadingScreen;
    [Title("Scene Components")]
    [OdinSerialize] private IInitializable[] _sceneComponents = Array.Empty<IInitializable>();

    private void Awake()
    {
        if(_loadingScreen) _loadingScreen.SetActive(true);
    }

    private async void Start()
    {
        
    }

    public async Task RunSetup()
    {
        if (_loadingScreen) _loadingScreen?.SetActive(true);
        foreach (var sceneComponent in _sceneComponents)
        {
            await sceneComponent.Init();
        }
    }


    public void CompleteSetup()
    {
        if (_loadingScreen) _loadingScreen?.SetActive(false);
    }
}


public sealed class SceneComponent : IInitializable
{

    [OdinSerialize, ReadOnly] private IInitializable[] _initializables;
    [Title("Prefab"), Tooltip("The level prefab to instantiate or initialize. Can Be gameobject or IInitializable asset.")]
    [SerializeField] UnityEngine.Object _prefab;

    [SerializeField, ReadOnly, ShowIf("ValidatePrefabGameObject")]
    private GameObject _instance;
    private bool ValidatePrefabGameObject()
    {
        if (_prefab == null)
        {
            return false;
        }

        return _prefab is GameObject;
    }

    public async Task Init()
    {
        List<IInitializable> initializables = new();
        if (ValidatePrefabGameObject())
        {
            GameObject go = _prefab as GameObject;

            _instance = Object.Instantiate(go);
            _instance.name = _prefab.name;
        }
        else if (_prefab is IInitializable initializable)
        {
            initializables.Add(initializable);
            await initializable.Init();
            _initializables = initializables.ToArray();
            return;
        }
        else
        {
            Debug.LogError("Prefab is not a GameObject or IInitializable assset");
            return;
        }

        if (_instance.GetComponentsInChildren<MonoBehaviour>().Length != 0)
        {
            foreach (var mono in _instance.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mono is not IInitializable initializable) continue;
                initializables.Add(initializable);
                await initializable.Init();
            }
        }

        _initializables = initializables.ToArray();
    }
}
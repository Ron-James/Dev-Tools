using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using CustomSceneReference;
using Sirenix.Serialization;
using UnityEngine.Serialization;

// Split interfaces
public interface ISceneLoadedListener
{
    void OnSceneLoaded(Scene scene, LoadSceneMode mode);
}

public interface ISceneUnloadedListener
{
    void OnSceneUnloaded(Scene scene);
}

public interface IActiveSceneChangedListener
{
    void OnActiveSceneChanged(Scene previous, Scene next);
}

public interface IEditorPlaymodeListener
{
    void OnEditorStopped();
}

// Legacy interfaces (kept for back-compat)
[Obsolete("Use ISceneLoadedListener and ISceneUnloadedListener.")]
public interface SceneLoadListener
{
    void OnSceneLoad(Scene scene);
    void OnSceneUnload(Scene scene);
}

[Obsolete("Use IEditorPlaymodeListener.")]
public interface EditorListener
{
    void OnEditorStopped();
}

[Obsolete("Use split interfaces: ISceneLoadedListener, ISceneUnloadedListener, IActiveSceneChangedListener, IEditorPlaymodeListener.")]
public interface ISceneCycleListener : EditorListener, SceneLoadListener
{
}

[CreateAssetMenu(fileName = "SceneLoadManager", menuName = "Scene Load Manager")]
public class SceneLoadManager : SerializedScriptableObject
{
    // Visible for debugging
    [ShowInInspector, ReadOnly] private static readonly List<ISceneLoadedListener> s_loadedListeners = new();
    [ShowInInspector, ReadOnly] private static readonly List<ISceneUnloadedListener> s_unloadedListeners = new();
    [ShowInInspector, ReadOnly] private static readonly List<IActiveSceneChangedListener> s_activeChangedListeners = new();
    [ShowInInspector, ReadOnly] private static readonly List<IEditorPlaymodeListener> s_editorListeners = new();
    [ShowInInspector, ReadOnly] private static readonly List<ISceneCycleListener> s_legacyCycleListeners = new();

    [SerializeReference, TableList] private List<SceneReference> _scenes = new();

    // Manual rebuild for debugging
    [Button(ButtonSizes.Medium)]
    private void RebuildNow() => RebuildListeners();

#if UNITY_EDITOR
    private void OnEnable()
    {
        _scenes.Clear();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            var sceneReference = new SceneReference(scene.path);
            _scenes.Add(sceneReference);
        }
        EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_loadedListeners.Clear();
        s_unloadedListeners.Clear();
        s_activeChangedListeners.Clear();
        s_editorListeners.Clear();
        s_legacyCycleListeners.Clear();
        // Ensure no duplicate subscriptions on domain reload
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= StaticEditorPlayModeStateChanged;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        RebuildListeners();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += StaticEditorPlayModeStateChanged;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitialNotify()
    {
        // Notify initial active scene as loaded (Single by default on startup)
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            NotifyLoaded(scene, LoadSceneMode.Single);
        }
    }

    private static void RebuildListeners()
    {
        s_loadedListeners.Clear();
        s_unloadedListeners.Clear();
        s_activeChangedListeners.Clear();
        s_editorListeners.Clear();
        s_legacyCycleListeners.Clear();

        var lookup = ScriptableObjectManagerAsset.Instance;
        foreach (var so in lookup.All())
        {
            if (so is ISceneLoadedListener ll) s_loadedListeners.Add(ll);
            if (so is ISceneUnloadedListener ul) s_unloadedListeners.Add(ul);
            if (so is IActiveSceneChangedListener al) s_activeChangedListeners.Add(al);
            if (so is IEditorPlaymodeListener el) s_editorListeners.Add(el);
            if (so is ISceneCycleListener legacy) s_legacyCycleListeners.Add(legacy);
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        NotifyLoaded(scene, mode);
    }

    private static void HandleSceneUnloaded(Scene scene)
    {
        NotifyUnloaded(scene);
    }

    private static void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        foreach (var listener in s_activeChangedListeners)
        {
            SafeInvoke(() => listener.OnActiveSceneChanged(oldScene, newScene), listener);
        }
    }

#if UNITY_EDITOR
    private static void StaticEditorPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            foreach (var listener in s_editorListeners)
            {
                SafeInvoke(() => listener.OnEditorStopped(), listener);
            }
            foreach (var legacy in s_legacyCycleListeners)
            {
                SafeInvoke(() => legacy.OnEditorStopped(), legacy);
            }
        }
    }

    private void OnEditorPlayModeStateChanged(PlayModeStateChange state)
    {
        StaticEditorPlayModeStateChanged(state);
    }
#endif

    private static void NotifyLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var listener in s_loadedListeners)
        {
            SafeInvoke(() => listener.OnSceneLoaded(scene, mode), listener);
        }
        // Legacy support
        foreach (var legacy in s_legacyCycleListeners)
        {
            SafeInvoke(() => legacy.OnSceneLoad(scene), legacy);
        }
    }

    private static void NotifyUnloaded(Scene scene)
    {
        foreach (var listener in s_unloadedListeners)
        {
            SafeInvoke(() => listener.OnSceneUnloaded(scene), listener);
        }
        // Legacy support
        foreach (var legacy in s_legacyCycleListeners)
        {
            SafeInvoke(() => legacy.OnSceneUnload(scene), legacy);
        }
    }

    private static void SafeInvoke(Action call, object listener)
    {
        try
        {
            call();
        }
        catch (Exception ex)
        {
            Debug.LogError($"SceneLoadManager error while notifying {listener?.GetType().Name}: {ex}");
        }
    }
}
// SPDX-License-Identifier: MIT
// Odin Unity Events — Samples
// File: OdinUnityEventDemo.cs
//
// Attach to a GameObject to test in the Inspector.
using UnityEngine;
#if HAS_ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
#endif

namespace OdinEvents.Samples
{
    public interface IInteractable { void Use(); }

    public sealed class OdinUnityEventDemo : SerializedMonoBehaviour
    {
#if HAS_ODIN_INSPECTOR
        [FoldoutGroup("On Start"), OdinSerialize] private OdinUnityEvent onStart = new();
        [FoldoutGroup("On Interact"), OdinSerialize] private OdinUnityEvent<IInteractable> onInteract = new();
        [FoldoutGroup("On Award"), OdinSerialize] private OdinUnityEvent<int, string> onAward = new();
#endif

        [Button] public void FireStart() => onStart.Invoke();
        [Button] public void FireInteract(IInteractable who) => onInteract.Invoke(who);
        [Button] public void FireAward(int score, string medal) => onAward.Invoke(score, medal);

        // Example receiver methods (this same component can be a target)
        private void Log() => Debug.Log("Start fired");
        private void HandleInteract(IInteractable x) => x?.Use();
        private void Award(int s, string medal) => Debug.Log($"Award {medal} for {s}");
    }
}

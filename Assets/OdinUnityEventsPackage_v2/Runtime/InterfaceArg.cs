// SPDX-License-Identifier: MIT
// Odin Unity Events — Runtime
// File: InterfaceArg.cs
//
// Purpose:
//   A unified picker for interface-typed constants that supports either a UnityEngine.Object
//   (Component/ScriptableObject) implementing the interface, or a managed reference (SerializeReference)
//   to a pure C# implementation.
//
// UX:
//   Toggle 'Use Unity Object' to switch between drag-and-drop Unity object and managed reference picker.
using System;
using UnityEngine;
#if HAS_ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
#endif

namespace OdinEvents
{
    [Serializable]
    public struct InterfaceArg<T> where T : class
    {
#if HAS_ODIN_INSPECTOR
        [LabelText("Use Unity Object")]
#endif
        public bool useUnityObject;

#if HAS_ODIN_INSPECTOR
        [ShowIf(nameof(useUnityObject))]
        [LabelText("Unity Ref")]
        [InterfaceSelector] // ensures the field only accepts objects implementing T
        [OdinSerialize]
#endif
        private UnityEngine.Object unityRef;

#if HAS_ODIN_INSPECTOR
        [HideIf(nameof(useUnityObject))]
        [LabelText("Managed (SerializeReference)")]
        [OdinSerialize, InlineProperty, HideLabel]
        [HideReferenceObjectPicker(false)]
#endif
        private T managed;

        public bool TryGet(out T value)
        {
            if (useUnityObject)
            {
                if (unityRef is T iface)
                {
                    value = iface;
                    return true;
                }

                if (unityRef is Component c && c is T compIface)
                {
                    value = compIface;
                    return true;
                }

                value = null;
                return false;
            }
            else
            {
                value = managed;
                return value != null;
            }
        }

        public override string ToString()
        {
            return useUnityObject
                ? (unityRef != null ? $"Unity: {unityRef.name}" : "Unity: (null)")
                : (managed != null ? $"Managed: {managed.GetType().Name}" : "Managed: (null)");
        }
    }
}

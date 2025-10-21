// filepath: c:\Unity\DevTools\Assets\OdinUnityEventsPackage_v2\Runtime\ConstantArg.cs
// SPDX-License-Identifier: MIT
// Odin Unity Events — Runtime
// File: ConstantArg.cs
//
// Purpose:
//   A generic constant argument container that works for primitives/value types,
//   strings, UnityEngine.Object types, and interfaces (managed reference via Odin).
//   This avoids imposing a class/interface constraint on T.
//
using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace OdinEvents
{
    [Serializable]
    public struct ConstantArg<T>
    {
        public enum Kind { Value, UnityObject, Managed }

        [OdinSerialize, LabelText("Mode"), OnValueChanged(nameof(OnKindChanged))]
        private Kind _kind;

        [OnInspectorInit]
        private void _Init()
        {
            if (_kind != 0) return;
            if (typeof(T).IsInterface || typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
                _kind = Kind.UnityObject;
            else
                _kind = Kind.Value;
        }

        [ShowIf(nameof(ShowValue)), OdinSerialize, LabelText("Value")]
        private T _value;

        [ShowIf(nameof(ShowUnityObject)), OdinSerialize, LabelText("Unity Object")]
        private UnityEngine.Object _unityRef;

        [ShowIf(nameof(ShowManaged)), OdinSerialize, LabelText("Managed (SerializeReference)")]
        private T _managed;

        // Draw guards
        public bool ShowValue => !typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) && !typeof(T).IsInterface;
        public bool ShowUnityObject => typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) || typeof(T).IsInterface;
        public bool ShowManaged => typeof(T).IsInterface;

        // Public accessor for mode (optional)
        public Kind Mode
        {
            get => _kind;
            set => _kind = value;
        }

        public bool TryGet(out T value)
        {
            switch (_kind)
            {
                case Kind.Value:
                    value = _value;
                    return true;
                case Kind.UnityObject:
                    if (_unityRef is T t)
                    {
                        value = t; return true;
                    }
                    if (_unityRef is Component c && c is T ifaceFromComponent)
                    {
                        value = ifaceFromComponent; return true;
                    }
                    value = default; return false;
                case Kind.Managed:
                    value = _managed;
                    return !Equals(value, default(T));
                default:
                    value = default; return false;
            }
        }

        private void OnKindChanged() { }

        public override string ToString()
        {
            return _kind switch
            {
                Kind.Value => _value != null ? _value.ToString() : "(null)",
                Kind.UnityObject => _unityRef != null ? _unityRef.name : "(null)",
                Kind.Managed => _managed != null ? _managed.ToString() : "(null)",
                _ => "(unknown)"
            };
        }
    }
}

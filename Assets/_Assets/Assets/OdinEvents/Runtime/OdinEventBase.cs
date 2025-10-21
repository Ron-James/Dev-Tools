// Assets/OdinEvents/Runtime/OdinEventBase.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace OdinEvents
{
    public enum ArgSourceKind { Constant = 0, EventArg0 = 1, EventArg1 = 2, EventArg2 = 3, EventArg3 = 4 }

    [Serializable]
    public sealed class SerializedConstant
    {
        [SerializeField] private UnityEngine.Object unityObject;

#if ODIN_INSPECTOR
        [OdinSerialize]
#endif
        [SerializeReference] private object value;

        public object GetValue() => (object)unityObject ?? value;

        public void SetValue(object v)
        {
            if (v is UnityEngine.Object uo) { unityObject = uo; value = null; }
            else { unityObject = null; value = v; }
        }
    }

    [Serializable]
    public sealed class ArgBinding
    {
        public ArgSourceKind source = ArgSourceKind.Constant;
        public SerializedConstant constant = new SerializedConstant();

        public object Resolve(object[] eventArgs)
        {
            switch (source)
            {
                case ArgSourceKind.Constant: return constant?.GetValue();
                case ArgSourceKind.EventArg0: return eventArgs != null && eventArgs.Length > 0 ? eventArgs[0] : null;
                case ArgSourceKind.EventArg1: return eventArgs != null && eventArgs.Length > 1 ? eventArgs[1] : null;
                case ArgSourceKind.EventArg2: return eventArgs != null && eventArgs.Length > 2 ? eventArgs[2] : null;
                case ArgSourceKind.EventArg3: return eventArgs != null && eventArgs.Length > 3 ? eventArgs[3] : null;
            }
            return null;
        }
    }

    public enum InterfaceArgSource { UnityObject = 0, InlineInstance = 1 }

    [Serializable]
    public sealed class InterfaceArg<TInterface>
    {
        [SerializeField] private InterfaceArgSource source = InterfaceArgSource.UnityObject;

#if AYSI
        [SerializeField] private AYellowpaper.SerializedInterface<TInterface> unityObject;
#else
        [SerializeField] private UnityEngine.Object unityObject;
#endif

#if SUBCLASS_SELECTOR
        [SerializeReference, Mackysoft.SerializeReferenceExtensions.SubclassSelector]
#else
        [SerializeReference]
#endif
        private TInterface inlineInstance;

        public TInterface GetValue()
        {
            if (source == InterfaceArgSource.InlineInstance) return inlineInstance;
#if AYSI
            return unityObject.Value;
#else
            if (unityObject is TInterface ti) return ti;
            if (unityObject is GameObject go)
            {
                foreach (var c in go.GetComponents<Component>())
                    if (c is TInterface t) return t;
            }
            return default;
#endif
        }

        public void SetInline(TInterface instance) { source = InterfaceArgSource.InlineInstance; inlineInstance = instance; }
#if AYSI
        public void SetUnityObject(AYellowpaper.SerializedInterface<TInterface> obj) { source = InterfaceArgSource.UnityObject; unityObject = obj; }
#else
        public void SetUnityObject(UnityEngine.Object obj) { source = InterfaceArgSource.UnityObject; unityObject = obj; }
#endif
    }

    public enum ArgExKind { None, Interface }

    [Serializable]
    public sealed class ArgBindingEx
    {
        [SerializeField] private ArgExKind kind = ArgExKind.None;
        [SerializeReference] private object boxedInterfaceArg; // InterfaceArg<T>

        public bool HasInterface => kind == ArgExKind.Interface;
        public void SetInterfaceBoxed(object interfaceArgBox) { kind = ArgExKind.Interface; boxedInterfaceArg = interfaceArgBox; }

        public object ResolveInterfaceValue()
        {
            if (boxedInterfaceArg == null) return null;
            var mi = boxedInterfaceArg.GetType().GetMethod("GetValue", BindingFlags.Instance | BindingFlags.Public);
            return mi?.Invoke(boxedInterfaceArg, null);
        }
    }

    [Serializable]
    public sealed class PersistentCall
    {
        public UnityEngine.Object target;
        public string methodName;
        public List<ArgBinding> arguments = new List<ArgBinding>();

        [NonSerialized] private MethodInfo _cachedMethod;

        public bool TryGetMethod(out MethodInfo method)
        {
            if (_cachedMethod != null) { method = _cachedMethod; return true; }
            method = null;
            if (target == null || string.IsNullOrEmpty(methodName)) return false;
            var type = target.GetType();
            method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method != null) { _cachedMethod = method; return true; }
            return false;
        }

        public void ClearCache() => _cachedMethod = null;

        public void Invoke(object[] eventArgs)
        {
            InvokeWithExtended(eventArgs, null);
        }

        public void InvokeWithExtended(object[] eventArgs, ArgBindingEx extended)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return;
            if (!TryGetMethod(out var method)) return;
            var paramInfos = method.GetParameters();
            int argCount = paramInfos.Length;
            object[] callArgs = argCount == 0 ? Array.Empty<object>() : new object[argCount];

            for (int i = 0; i < argCount; i++)
            {
                object value = null;
                if (extended != null && extended.HasInterface) value = extended.ResolveInterfaceValue();
                else value = (i < arguments.Count) ? arguments[i].Resolve(eventArgs) : null;

                var expected = paramInfos[i].ParameterType;
                if (value != null && expected != typeof(object))
                {
                    try
                    {
                        if (!expected.IsInstanceOfType(value))
                        {
                            if (expected.IsEnum && value is string s) value = Enum.Parse(expected, s, true);
                            else value = Convert.ChangeType(value, expected);
                        }
                    }
                    catch { /* ignore */ }
                }
                callArgs[i] = value;
            }

            method.Invoke(target, callArgs);
        }
    }

    [Serializable]
    public class OdinEventBase
    {
        [ShowInInspector]
        [SerializeField] private List<PersistentCall> _persistentCalls = new List<PersistentCall>();
        [SerializeField] private List<ArgBindingEx> _extendedBindings = new List<ArgBindingEx>();

        public IReadOnlyList<PersistentCall> PersistentCalls => _persistentCalls;
        public List<ArgBindingEx> ExtendedBindings => _extendedBindings;

        public void AddListener(UnityEngine.Object target, string methodName, params ArgBinding[] args)
        {
            var call = new PersistentCall { target = target, methodName = methodName };
            if (args != null) call.arguments.AddRange(args);
            _persistentCalls.Add(call);
            _extendedBindings.Add(new ArgBindingEx());
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _persistentCalls.Count)
            {
                _persistentCalls.RemoveAt(index);
                if (index < _extendedBindings.Count) _extendedBindings.RemoveAt(index);
            }
        }

        protected void InvokeInternal(params object[] args)
        {
            var calls = _persistentCalls.ToArray();
            var exts  = _extendedBindings.ToArray();
            for (int i = 0; i < calls.Length; i++)
            {
                try { calls[i].InvokeWithExtended(args, i < exts.Length ? exts[i] : null); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }

    [Serializable] public sealed class OdinEvent : OdinEventBase { public void Invoke() => InvokeInternal(); }
    [Serializable] public sealed class OdinEvent<T0> : OdinEventBase { public void Invoke(T0 a0) => InvokeInternal(a0); }
    [Serializable] public sealed class OdinEvent<T0, T1> : OdinEventBase { public void Invoke(T0 a0, T1 a1) => InvokeInternal(a0, a1); }
    [Serializable] public sealed class OdinEvent<T0, T1, T2> : OdinEventBase { public void Invoke(T0 a0, T1 a1, T2 a2) => InvokeInternal(a0, a1, a2); }
    [Serializable] public sealed class OdinEvent<T0, T1, T2, T3> : OdinEventBase { public void Invoke(T0 a0, T1 a1, T2 a2, T3 a3) => InvokeInternal(a0, a1, a2, a3); }
}

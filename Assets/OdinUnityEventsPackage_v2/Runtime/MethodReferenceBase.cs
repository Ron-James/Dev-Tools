// SPDX-License-Identifier: MIT
// Odin Unity Events — Runtime
// File: MethodReferenceBase.cs
//
// Purpose:
//   UnityEvent-like method reference powered by Odin serialization.
//   Stores a UnityEngine.Object target + a validated method name and resolves
//   to a cached delegate at runtime; falls back to reflection if necessary.
//
// Key design:
//   * Signature filtering with contravariant-safe check for interface params
//     (listener parameter type must be assignable FROM the event's generic type).
//   * Works with private methods via reflection fallback.
//
// Requirements:
//   * Odin Inspector + Serializer
//
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace OdinEvents
{
    [Serializable]
    public abstract class MethodReferenceBase
    {
        [OdinSerialize, Required, HorizontalGroup("h1", Width = 250), LabelText("Target"), InlineButton(nameof(ClearTarget), "✕")]
        private UnityEngine.Object _target;

        [OdinSerialize, HorizontalGroup("h1"), LabelText("Method"), ValueDropdown(nameof(GetMethodDropdown)), OnValueChanged(nameof(OnMethodPicked))]
        private string _methodName;

        [NonSerialized] private bool _resolved;
        [NonSerialized] private MethodInfo _method;
        [NonSerialized] private Delegate _cachedDelegate;
        [NonSerialized] private string _validationMessage;

        [ShowIf(nameof(HasValidation)), InfoBox("@_validationMessage", InfoMessageType.Error)]
        [HideLabel, ReadOnly]
        public string Validation => _validationMessage;
        private bool HasValidation => !string.IsNullOrEmpty(_validationMessage);

        protected abstract Type[] Signature();
        protected abstract Type ReturnType();

        protected object Target => _target;
        protected MethodInfo Method => _method;

        private void ClearTarget()
        {
            _target = null;
            _methodName = null;
            Invalidate();
        }

        protected void Invalidate()
        {
            _resolved = false;
            _method = null;
            _cachedDelegate = null;
            _validationMessage = null;
        }

        private ValueDropdownList<string> GetMethodDropdown()
        {
            var list = new ValueDropdownList<string>();
            if (_target == null)
            {
                list.Add("(assign a target)", null);
                return list;
            }

            var sig = Signature();
            var ret = ReturnType();

            void AddMethodsFor(UnityEngine.Object obj, Type type, string ownerLabel, string valueOwner)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var methods = type.GetMethods(flags)
                    .Where(m => !m.IsAbstract && !m.IsGenericMethodDefinition)
                    .Where(m => ret == null || m.ReturnType == ret)
                    .Where(m => IsCompatibleSignatureFlexible(m, sig))
                    .OrderBy(m => m.Name)
                    .ThenBy(m => m.GetParameters().Length);

                foreach (var m in methods)
                {
                    var ps = m.GetParameters();
                    var paramLabels = string.Join(", ", ps.Select(p => $"{p.Name}: {GetFriendlyTypeName(p.ParameterType)}"));
                    var label = $"{ownerLabel}.{m.Name}({paramLabels})";
                    string value;
                    if (valueOwner == "self")
                        value = $"self||{m.Name}";
                    else
                        value = $"comp||{valueOwner}||{m.Name}";
                    list.Add(label, value);
                }
            }

            if (_target is GameObject go)
            {
                // Methods on GameObject itself
                AddMethodsFor(go, typeof(GameObject), nameof(GameObject), "self");

                // Methods on each component (include index to disambiguate duplicates)
                var components = go.GetComponents<Component>();
                var typeCounts = new System.Collections.Generic.Dictionary<Type, int>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    var t = comp.GetType();
                    typeCounts.TryGetValue(t, out var idx);
                    typeCounts[t] = idx + 1;
                    var ownerLabel = t.Name;
                    var valueOwner = $"{t.AssemblyQualifiedName}||{idx}";
                    AddMethodsFor(comp, t, ownerLabel, valueOwner);
                }
            }
            else if (_target is Component c)
            {
                // Only this component's methods
                AddMethodsFor(c, c.GetType(), c.GetType().Name, "self");
            }
            else
            {
                // ScriptableObject or other UnityEngine.Object
                var t = _target.GetType();
                AddMethodsFor(_target, t, t.Name, "self");
            }

            if (list.Count == 0)
                list.Add("(no compatible methods)", null);

            // Also add a passthrough item for the current raw value so it remains selected/highlighted
            if (!string.IsNullOrEmpty(_methodName) && !list.Any(i => i.Value == _methodName))
            {
                list.Add($"(current) {_methodName}", _methodName);
            }

            return list;
        }

        // Check if method parameters can accept values of the provided signature types
        private static bool IsCompatibleSignature(MethodInfo m, Type[] sig)
        {
            var ps = m.GetParameters();
            if (ps.Length != sig.Length) return false;
            for (int i = 0; i < ps.Length; i++)
            {
                if (!ps[i].ParameterType.IsAssignableFrom(sig[i])) return false;
            }
            return true;
        }

        private static bool IsCompatibleSignatureFlexible(MethodInfo m, Type[] sig)
        {
            var ps = m.GetParameters();
            if (ps.Length == 0 && sig.Length > 0) return true; // allow zero-arg targets
            if (ps.Length > sig.Length) return false;
            for (int i = 0; i < ps.Length; i++)
            {
                if (!ps[i].ParameterType.IsAssignableFrom(sig[i])) return false;
            }
            return true;
        }

        private void OnMethodPicked()
        {
            if (string.IsNullOrEmpty(_methodName) || _target == null) return;
            // Handle encoded selection: "self||Method" or "comp||{assemblyQualifiedType}||{index}||Method"
            var parts = _methodName.Split(new[] { "||" }, StringSplitOptions.None);
            if (parts.Length >= 2 && parts[0] == "self")
            {
                _methodName = parts[1];
                Invalidate();
                return;
            }
            if (parts.Length >= 4 && parts[0] == "comp")
            {
                var typeName = parts[1];
                var indexStr = parts[2];
                var method = parts[3];
                var type = Type.GetType(typeName);
                int index = 0;
                int.TryParse(indexStr, out index);

                GameObject go = null;
                if (_target is GameObject goT) go = goT;
                else if (_target is Component compT) go = compT.gameObject;

                if (go != null && type != null)
                {
                    var comps = go.GetComponents(type);
                    if (comps != null && comps.Length > 0)
                    {
                        var chosen = index >= 0 && index < comps.Length ? comps[index] : comps[0];
                        _target = chosen;
                        _methodName = method;
                        Invalidate();
                        return;
                    }
                }
                // Fallback: strip encoding if resolution failed
                _methodName = method;
                Invalidate();
            }
        }

        protected bool TryResolve()
        {
            if (_resolved) return _method != null;
            _validationMessage = null;
            if (_target == null) { _validationMessage = "Assign a target."; _resolved = true; return false; }
            if (string.IsNullOrEmpty(_methodName)) { _validationMessage = "Pick a method."; _resolved = true; return false; }

            var type = _target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var sig = Signature();
            var ret = ReturnType();

            _method = type.GetMethod(_methodName, flags, null, sig, null);
            if (_method == null)
            {
                var candidates = type.GetMethods(flags)
                    .Where(m => m.Name == _methodName)
                    .Where(m => !m.IsAbstract && !m.IsGenericMethodDefinition)
                    .Where(m => ret == null || m.ReturnType == ret)
                    .Where(m => IsCompatibleSignatureFlexible(m, sig))
                    .OrderByDescending(m => m.GetParameters().Length) // prefer most-arg match first
                    .FirstOrDefault();
            }

            if (_method == null) { _validationMessage = $"Method '{_methodName}' not found or incompatible on {type.Name}."; _resolved = true; return false; }
            if (ReturnType() != null && _method.ReturnType != ReturnType()) { _validationMessage = "Return type mismatch."; _resolved = true; return false; }
            _resolved = true; return true;
        }

        protected bool TryCreateDelegate(Type delegateType, out Delegate del)
        {
            del = null;
            if (!TryResolve()) return false;
            try
            {
                _cachedDelegate ??= Delegate.CreateDelegate(delegateType, Target, Method, true);
                del = _cachedDelegate;
                if (del == null) throw new Exception("CreateDelegate returned null.");
                return true;
            }
            catch
            {
                return false; // use reflection fallback
            }
        }

        private static string GetFriendlyTypeName(Type t)
        {
            if (t == typeof(void)) return "void";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(int)) return "int";
            if (t == typeof(float)) return "float";
            if (t == typeof(double)) return "double";
            if (t == typeof(string)) return "string";
            if (t.IsGenericType)
            {
                var name = t.Name;
                var tick = name.IndexOf('`');
                if (tick >= 0) name = name.Substring(0, tick);
                var args = t.GetGenericArguments().Select(GetFriendlyTypeName);
                return $"{name}<{string.Join(", ", args)}>`".TrimEnd('`');
            }
            return t.Name;
        }

        protected void InvokeResolved(params object[] args)
        {
            if (!TryResolve()) return;
            var pc = _method.GetParameters().Length;
            if (pc == 0)
            {
                _method.Invoke(Target, null);
                return;
            }
            if (args == null)
            {
                throw new TargetParameterCountException($"Method expects {pc} argument(s) but got 0.");
            }
            if (args.Length != pc)
            {
                if (args.Length > pc)
                {
                    var trimmed = new object[pc];
                    Array.Copy(args, trimmed, pc);
                    _method.Invoke(Target, trimmed);
                    return;
                }
                throw new TargetParameterCountException($"Method expects {pc} argument(s) but got {args.Length}.");
            }
            _method.Invoke(Target, args);
        }
    }
}

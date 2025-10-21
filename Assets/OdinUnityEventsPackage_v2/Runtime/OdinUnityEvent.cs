// SPDX-License-Identifier: MIT
// Odin Unity Events — Runtime
// File: OdinUnityEvent.cs
//
// Public surface:
//   - OdinUnityEvent
//   - OdinUnityEvent<T1>
//   - OdinUnityEvent<T1,T2>
//   - OdinUnityEvent<T1,T2,T3>
//
// Notes:
//   * Per-listener flags: Enabled, Once, Order (stable execution order)
//   * Per-argument source: FromInvoke | Constant
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace OdinEvents
{
    public enum ArgumentSource { FromInvoke, Constant }

    // -------------------- 0 ARG --------------------
    [Serializable]
    public class OdinUnityEvent
    {
        [Serializable]
        public class Listener : MethodReferenceBase
        {
            [OdinSerialize, LabelText("Enabled")] public bool Enabled = true;
            [OdinSerialize, LabelText("Once")] public bool Once;
            [OdinSerialize, LabelText("Order"), MinValue(0)] public int Order;

            protected override Type[] Signature() => Type.EmptyTypes;
            protected override Type ReturnType() => typeof(void);

            public void Invoke()
            {
                if (!Enabled) return;
                InvokeResolved();
            }
        }

        [OdinSerialize, ListDrawerSettings(Expanded = true, DraggableItems = true)]
        private List<Listener> _listeners = new();

        public void Invoke()
        {
            _listeners.Sort((a, b) => a.Order.CompareTo(b.Order));
            var toRemove = new List<Listener>();
            foreach (var l in _listeners)
            {
                try { l.Invoke(); if (l.Once) toRemove.Add(l); }
                catch (Exception e) { Debug.LogException(e); }
            }
            foreach (var l in toRemove) _listeners.Remove(l);
        }
    }

    // -------------------- 1 ARG --------------------
    [Serializable]
    public class OdinUnityEvent<T1>
    {
        [Serializable]
        public class Listener : MethodReferenceBase
        {
            [OdinSerialize, LabelText("Enabled")] public bool Enabled = true;
            [OdinSerialize, LabelText("Once")] public bool Once;
            [OdinSerialize, LabelText("Order"), MinValue(0)] public int Order;

            [OdinSerialize, LabelText("Arg1 Source")] public ArgumentSource Arg1Source = ArgumentSource.FromInvoke;
            [ShowIf("@Arg1Source == ArgumentSource.Constant"), OdinSerialize, LabelText("Arg1 (Constant)")] public ConstantArg<T1> Arg1;

            protected override Type[] Signature() => new[] { typeof(T1) };
            protected override Type ReturnType() => typeof(void);

            public void Invoke(T1 a1)
            {
                if (!Enabled) return;
                var v1 = a1;
                if (Arg1Source == ArgumentSource.Constant)
                {
                    if (!Arg1.TryGet(out v1))
                    {
                        Debug.LogError($"[OdinUnityEvent] Arg1 constant not valid for {typeof(T1).Name}"); return;
                    }
                }
                InvokeResolved(v1);
            }
        }

        [OdinSerialize, ListDrawerSettings(Expanded = true, DraggableItems = true)]
        private List<Listener> _listeners = new();

        public void Invoke(T1 a1)
        {
            _listeners.Sort((a, b) => a.Order.CompareTo(b.Order));
            var toRemove = new List<Listener>();
            foreach (var l in _listeners)
            {
                try { l.Invoke(a1); if (l.Once) toRemove.Add(l); }
                catch (Exception e) { Debug.LogException(e); }
            }
            foreach (var l in toRemove) _listeners.Remove(l);
        }
    }

    // -------------------- 2 ARG --------------------
    [Serializable]
    public class OdinUnityEvent<T1, T2>
    {
        [Serializable]
        public class Listener : MethodReferenceBase
        {
            [OdinSerialize, LabelText("Enabled")] public bool Enabled = true;
            [OdinSerialize, LabelText("Once")] public bool Once;
            [OdinSerialize, LabelText("Order"), MinValue(0)] public int Order;

            [OdinSerialize, LabelText("Arg1 Source")] public ArgumentSource Arg1Source = ArgumentSource.FromInvoke;
            [ShowIf("@Arg1Source == ArgumentSource.Constant"), OdinSerialize, LabelText("Arg1 (Constant)")] public ConstantArg<T1> Arg1;

            [OdinSerialize, LabelText("Arg2 Source")] public ArgumentSource Arg2Source = ArgumentSource.FromInvoke;
            [ShowIf("@Arg2Source == ArgumentSource.Constant"), OdinSerialize, LabelText("Arg2 (Constant)")] public ConstantArg<T2> Arg2;

            protected override Type[] Signature() => new[] { typeof(T1), typeof(T2) };
            protected override Type ReturnType() => typeof(void);

            public void Invoke(T1 a1, T2 a2)
            {
                if (!Enabled) return;
                var v1 = a1; var v2 = a2;

                if (Arg1Source == ArgumentSource.Constant && !Arg1.TryGet(out v1))
                { Debug.LogError($"[OdinUnityEvent] Arg1 constant not valid for {typeof(T1).Name}"); return; }

                if (Arg2Source == ArgumentSource.Constant && !Arg2.TryGet(out v2))
                { Debug.LogError($"[OdinUnityEvent] Arg2 constant not valid for {typeof(T2).Name}"); return; }

                InvokeResolved(v1, v2);
            }
        }

        [OdinSerialize, ListDrawerSettings(Expanded = true, DraggableItems = true)]
        private List<Listener> _listeners = new();

        public void Invoke(T1 a1, T2 a2)
        {
            _listeners.Sort((a, b) => a.Order.CompareTo(b.Order));
            var toRemove = new List<Listener>();
            foreach (var l in _listeners)
            {
                try { l.Invoke(a1, a2); if (l.Once) toRemove.Add(l); }
                catch (Exception e) { Debug.LogException(e); }
            }
            foreach (var l in toRemove) _listeners.Remove(l);
        }
    }

    // -------------------- 3 ARG --------------------
    [Serializable]
    public class OdinUnityEvent<T1, T2, T3>
    {
        [Serializable]
        public class Listener : MethodReferenceBase
        {
            [OdinSerialize, LabelText("Enabled")] public bool Enabled = true;
            [OdinSerialize, LabelText("Once")] public bool Once;
            [OdinSerialize, LabelText("Order"), MinValue(0)] public int Order;

            [OdinSerialize, LabelText("Arg1 Source")] public ArgumentSource Arg1Source = ArgumentSource.FromInvoke;
            [ShowIf("@Arg1Source == ArgumentSource.Constant"), OdinSerialize, LabelText("Arg1 (Constant)")] public ConstantArg<T1> Arg1;

            [OdinSerialize, LabelText("Arg2 Source")] public ArgumentSource Arg2Source = ArgumentSource.FromInvoke;
            [ShowIf("@Arg2Source == ArgumentSource.Constant"), OdinSerialize, LabelText("Arg2 (Constant)")] public ConstantArg<T2> Arg2;

            [OdinSerialize, LabelText("Arg3 Source")] public ArgumentSource Arg3Source = ArgumentSource.FromInvoke;
            [ShowIf("@Arg3Source == ArgumentSource.Constant"), OdinSerialize, LabelText("Arg3 (Constant)")] public ConstantArg<T3> Arg3;

            protected override Type[] Signature() => new[] { typeof(T1), typeof(T2), typeof(T3) };
            protected override Type ReturnType() => typeof(void);

            public void Invoke(T1 a1, T2 a2, T3 a3)
            {
                if (!Enabled) return;
                var v1 = a1; var v2 = a2; var v3 = a3;

                if (Arg1Source == ArgumentSource.Constant && !Arg1.TryGet(out v1))
                { Debug.LogError($"[OdinUnityEvent] Arg1 constant not valid for {typeof(T1).Name}"); return; }

                if (Arg2Source == ArgumentSource.Constant && !Arg2.TryGet(out v2))
                { Debug.LogError($"[OdinUnityEvent] Arg2 constant not valid for {typeof(T2).Name}"); return; }

                if (Arg3Source == ArgumentSource.Constant && !Arg3.TryGet(out v3))
                { Debug.LogError($"[OdinUnityEvent] Arg3 constant not valid for {typeof(T3).Name}"); return; }

                InvokeResolved(v1, v2, v3);
            }
        }

        [OdinSerialize, ListDrawerSettings(Expanded = true, DraggableItems = true)]
        private List<Listener> _listeners = new();

        public void Invoke(T1 a1, T2 a2, T3 a3)
        {
            _listeners.Sort((a, b) => a.Order.CompareTo(b.Order));
            var toRemove = new List<Listener>();
            foreach (var l in _listeners)
            {
                try { l.Invoke(a1, a2, a3); if (l.Once) toRemove.Add(l); }
                catch (Exception e) { Debug.LogException(e); }
            }
            foreach (var l in toRemove) _listeners.Remove(l);
        }
    }
}

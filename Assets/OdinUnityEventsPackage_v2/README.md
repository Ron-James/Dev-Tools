# Odin Unity Events (UnityEvent, beefed up with Odin) — v2 (Interface-friendly)

**What:** UnityEvent-like events that play perfectly with Odin, including interface parameters that can be satisfied by either **UnityEngine.Object** implementers or **plain C#** managed objects.

## Highlights
- `OdinUnityEvent`, `OdinUnityEvent<T1>`, `OdinUnityEvent<T1,T2>`, `OdinUnityEvent<T1,T2,T3>`
- Per-listener: **Enabled**, **Once**, **Order**
- Per-argument source: **FromInvoke** (runtime) or **Constant**
- **Interface constants** via `InterfaceArg<T>`:
  - Drag Components/ScriptableObjects that implement the interface (`[InterfaceSelector]`)
  - Or pick a pure C# implementer via **SerializeReference** managed reference
- Safer interface filtering: methods are listed only if their parameter types are **assignable from** the event’s generic types (contravariant-safe)
- Cached delegates; reflection fallback for private methods
- IL2CPP support via `link.xml`

## Requirements
- Unity 2020.3+
- Odin Inspector + Serializer (fields use `[OdinSerialize]` or inherit `SerializedMonoBehaviour` / `SerializedScriptableObject`)

## Quick Start
```csharp
using OdinEvents;
using Sirenix.Serialization;
using UnityEngine;

public class MyCaller : SerializedMonoBehaviour
{
    [OdinSerialize] private OdinUnityEvent onStart = new();
    [OdinSerialize] private OdinUnityEvent<int> onScore = new();
    [OdinSerialize] private OdinUnityEvent<IMyInterface> onDoThing = new();

    void Start()
    {
        onStart.Invoke();
        onScore.Invoke(123);
        onDoThing.Invoke(new SomeImplementer()); // or supply constant via inspector
    }
}
```

In the Inspector:
1. Add a listener (drag a target object).
2. Pick a compatible method (signature-checked).
3. For interface parameters with Constant source, toggle between **Unity Ref** or **Managed** picker.

## Extending
- Copy the pattern for `OdinUnityEvent<T1,T2,T3,T4>` and beyond.
- Add Editor drawers under the `Editor` assembly to mimic UnityEvent’s exact look & feel.

## License
MIT

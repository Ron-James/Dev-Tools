using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class TestMono : SerializedMonoBehaviour
{
    [OdinSerialize] IGuidReference _guidReference;
}

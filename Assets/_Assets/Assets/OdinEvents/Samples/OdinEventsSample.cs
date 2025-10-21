// Assets/OdinEvents/Samples/OdinEventsSample.cs
using UnityEngine;

namespace OdinEvents.Samples
{
    public class OdinEventsSample : MonoBehaviour
    {
        public OdinEvents.OdinEvent onScored;

        [ContextMenu("Test Invoke")]
        private void TestInvoke()
        {
            onScored.Invoke();
        }

        public void LogWithScore(IScorer scorer, string label, int value)
        {
            var s = scorer?.Score(label, value) ?? -1;
            Debug.Log($"[{name}] {label}:{value} -> score {s}");
        }
    }
}

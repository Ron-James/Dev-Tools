// Assets/OdinEvents/Samples/IScorer.cs
using System;
public interface IScorer { int Score(string label, int value); }

[Serializable]
public class SumScorer : IScorer
{
    public int bonus;
    public int Score(string label, int value) => value + bonus;
}

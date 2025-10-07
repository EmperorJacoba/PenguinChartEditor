using System.Collections.Generic;
using UnityEngine;

public static class Tempo
{
    public static SortedDictionary<int, BPMData> Events { get; set; } = new();
}
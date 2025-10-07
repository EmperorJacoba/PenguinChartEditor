using UnityEngine;
using System.Collections.Generic;

public static class TimeSignature
{
    public static SortedDictionary<int, TSData> Events { get; set; } = new();
}
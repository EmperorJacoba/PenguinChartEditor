using UnityEngine;
using System.Collections.Generic;

public class EventData<T>
{
    public SortedDictionary<int, T> Events { get; set; } = new();
    public HashSet<int> Selection { get; set; } = new();
    public SortedDictionary<int, T> Clipboard = new();
}
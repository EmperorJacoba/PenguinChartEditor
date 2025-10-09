using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A collection of event related data that all distinct event types must have, like selection, clipboard, and event data sets.
/// </summary>
/// <typeparam name="T">The type of event data this object holds (ex. BPMData)</typeparam>
public class EventData<T> where T : IEventData
{
    public SortedDictionary<int, T> Selection { get; set; } = new();
    public SortedDictionary<int, T> Clipboard = new();
    public bool RMBHeld = false;
}
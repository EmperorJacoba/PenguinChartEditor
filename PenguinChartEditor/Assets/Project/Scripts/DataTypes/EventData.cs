using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A collection of event related data that all distinct event types must have, like selection, clipboard, and event data sets.
/// </summary>
/// <typeparam name="T">The type of event data this object holds (ex. BPMData)</typeparam>
public class EventData<T> where T : IEventData
{
    public SortedDictionary<int, T> Events { get; set; } = new();
    public HashSet<int> Selection { get; set; } = new();
    public SortedDictionary<int, T> Clipboard = new();
    public SortedDictionary<int, T> MovingGhostSet = new();
    public Move<T> currentMoveAction;

    public bool selectionActionsEnabled = false;
}
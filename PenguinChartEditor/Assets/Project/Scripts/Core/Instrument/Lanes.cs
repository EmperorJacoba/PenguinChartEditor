using UnityEngine;
using System.Collections.Generic;

public class Lanes<T> where T : IEventData
{
    SortedDictionary<int, T>[] lanes;
    SelectionSet<T>[] selections;
    ClipboardSet<T>[] clipboards;
    public HashSet<int> TempSustainTicks = new();

    public Lanes(int laneCount)
    {
        lanes = new SortedDictionary<int, T>[laneCount];
        selections = new SelectionSet<T>[laneCount];
        clipboards = new ClipboardSet<T>[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            lanes[i] = new();
            selections[i] = new(lanes[i]);
            clipboards[i] = new(lanes[i]);
        }
    }

    public SortedDictionary<int, T> GetLane(int lane) => lanes[lane];
    public void SetLane(int lane, SortedDictionary<int, T> newData) => lanes[lane] = newData;
    public SelectionSet<T> GetLaneSelection(int lane) => selections[lane];
    public ClipboardSet<T> GetLaneClipboard(int lane) => clipboards[lane];


    public int Count => lanes.Length;
}
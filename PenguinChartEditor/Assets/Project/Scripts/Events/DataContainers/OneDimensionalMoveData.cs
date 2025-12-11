using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// One dimension = time movement only. For things like BPM/TS.
public class OneDimensionalMoveData<T> where T : IEventData
{
    public readonly bool inProgress;

    public readonly int firstMouseTick;
    public int lastMouseTick;
    public readonly int firstSelectionTick;

    public SortedDictionary<int, T> originalMovingDataSet;
    public SortedDictionary<int, T> preMoveData;

    public int lastGhostStartTick;
    public int lastGhostEndTick
    {
        get
        {
            return lastGhostStartTick + originalMovingDataSet.Keys.Max();
        }
    }

    public OneDimensionalMoveData(
        int currentMouseTick, 
        SortedDictionary<int, T> laneData, 
        SortedDictionary<int, T> selectionData, 
        int firstSelectionTick
        )
    {
        firstMouseTick = currentMouseTick;
        lastMouseTick = currentMouseTick;

        this.firstSelectionTick = firstSelectionTick;
        if (firstSelectionTick == SelectionSet<T>.NONE_SELECTED) return;

        lastGhostStartTick = firstSelectionTick;

        preMoveData = laneData;
        originalMovingDataSet = selectionData;

        foreach(var tick in originalMovingDataSet.Keys)
        {
            preMoveData.Remove(tick + firstSelectionTick, out T data);
        }

        inProgress = true;
    }

    public OneDimensionalMoveData()
    {
        inProgress = false;
    }
}
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
        LaneSet<T> lane, 
        SortedDictionary<int, T> selectionData, 
        int firstSelectionTick
        )
    {
        firstMouseTick = currentMouseTick;
        lastMouseTick = currentMouseTick;

        this.firstSelectionTick = firstSelectionTick;
        if (firstSelectionTick == SelectionSet<T>.NONE_SELECTED || selectionData.Count == 0) return;

        lastGhostStartTick = firstSelectionTick;

        preMoveData = lane.ExportData();
        originalMovingDataSet = selectionData;

        foreach (var tick in lane.protectedTicks)
        {
            if (originalMovingDataSet.ContainsKey(tick - firstSelectionTick))
            {
                originalMovingDataSet.Remove(tick - firstSelectionTick);
            }
        }

        foreach (var tick in originalMovingDataSet.Keys)
        {
            Chart.Log($"{tick}");
            preMoveData.Remove(tick + firstSelectionTick, out T data);
        }

        inProgress = true;
    }

    public OneDimensionalMoveData()
    {
        inProgress = false;
    }
}
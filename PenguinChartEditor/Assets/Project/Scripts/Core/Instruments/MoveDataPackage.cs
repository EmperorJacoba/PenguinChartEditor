
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Overlays;

public class MoveDataPackage<T> where T : IEventData
{
    public bool inProgress = false;

    public int firstMouseTick;
    public int lastMouseTick;
    public int firstSelectionTick;

    public SortedDictionary<int, T>[] movingData;
    public SortedDictionary<int, T>[] preMoveData;
    public int lastGhostStartTick;
    public int lastGhostEndTick
    {
        get
        {
            HashSet<int> maxTicks = new();
            foreach (var lane in movingData)
            {
                if (lane.Count > 0) maxTicks.Add(lane.Keys.Max());
            }
            return lastGhostStartTick + maxTicks.Max();
        }
    }

    public MoveDataPackage(int currentMouseTick, SortedDictionary<int, T>[] laneData, SortedDictionary<int, T>[] selectionData, int firstSelectionTick)
    {
        firstMouseTick = currentMouseTick;
        lastMouseTick = currentMouseTick;

        this.firstSelectionTick = firstSelectionTick;
        if (firstSelectionTick == SelectionSet<T>.NONE_SELECTED)
        {
            return;
        }
        lastGhostStartTick = firstSelectionTick;

        // preMoveData needs selection data removed
        preMoveData = laneData;
        movingData = selectionData;

        for (int i = 0; i < preMoveData.Length; i++)
        {
            foreach (var tick in movingData[i].Keys)
            {
                preMoveData[i].Remove(tick + firstSelectionTick, out T data);
            }
        }

        inProgress = true;
    }

    public HashSet<int>[] GetKeysToReAdd()
    {
        HashSet<int>[] receiver = new HashSet<int>[preMoveData.Length];
        for (int i = 0; i < preMoveData.Length; i++)
        {
            receiver[i] = preMoveData[i].Keys.Where(x => x >= lastGhostStartTick && x <= lastGhostEndTick).ToHashSet();
        }
        return receiver;
    }

    public MoveDataPackage()
    {
        inProgress = false;
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Two dimensions in this scenario meaning across time and lanes. Enables lane-to-lane movement.
public class UniversalMoveData<T> where T : IEventData
{
    public bool inProgress = false;

    public readonly int firstMouseTick;
    public int lastMouseTick;
    public readonly int firstLane;
    public int lastLane;
    public readonly int firstSelectionTick;

    /// <summary>
    /// Contains the original (normalized) set of data that is being moved.
    /// </summary>
    readonly Dictionary<int, SortedDictionary<int, T>> originalMovingDataSet;

    /// <summary>
    /// Contains the original chart data with the data being moved deleted.
    /// </summary>
    public readonly Dictionary<int, SortedDictionary<int, T>> preMoveData;

    public int lastGhostStartTick;
    public int lastGhostEndTick
    {
        get
        {
            HashSet<int> maxTicks = new();
            foreach (var lane in originalMovingDataSet)
            {
                if (lane.Value.Count > 0) maxTicks.Add(lane.Value.Keys.Max());
            }
            return lastGhostStartTick + maxTicks.Max();
        }
    }

    /// <summary>
    /// Use when operating on a 2D instrument lane set. 
    /// </summary>
    /// <param name="currentMouseTick"></param>
    /// <param name="currentLane"></param>
    /// <param name="laneData">Use Lanes.ExportData() to get this (or data of a similar kind)</param>
    /// <param name="selectionData">Use Lanes.ExportNormalizedSelection() (or data of a similar kind)</param>
    /// <param name="firstSelectionTick"></param>
    public UniversalMoveData(
        int currentMouseTick,
        int currentLane,
        Dictionary<int, SortedDictionary<int, T>> laneData,
        Dictionary<int, SortedDictionary<int, T>> selectionData,
        int firstSelectionTick
        )
    {
        firstMouseTick = currentMouseTick;
        lastMouseTick = currentMouseTick;
        firstLane = currentLane;

        this.firstSelectionTick = firstSelectionTick;
        if (firstSelectionTick == SelectionSet<T>.NONE_SELECTED) return;

        lastGhostStartTick = firstSelectionTick;

        // preMoveData needs selection data removed
        preMoveData = laneData;
        originalMovingDataSet = selectionData;

        foreach (var preMoveLane in preMoveData)
        {
            foreach (var tick in originalMovingDataSet[preMoveLane.Key].Keys)
            {
                preMoveLane.Value.Remove(tick + firstSelectionTick);
            }
        }

        inProgress = true;
    }

    /// <summary>
    /// Use when operating on one a single lane. 2D lane needs like lane shifting are waived. Use preMoveData[0] to reference the reference data.
    /// </summary>
    /// <param name="currentMouseTick"></param>
    /// <param name="laneSet">The LaneSet corresponding to this lane.</param>
    /// <param name="selection">The SelectionSet corresponding to this lane.</param>
    public UniversalMoveData(
        int currentMouseTick,
        LaneSet<T> laneSet,
        SelectionSet<T> selection
        )
    {
        firstMouseTick = currentMouseTick;
        lastMouseTick = currentMouseTick;

        firstSelectionTick = selection.GetFirstSelectedTick();
        if (firstSelectionTick == SelectionSet<T>.NONE_SELECTED || selection.Count == 0) return;

        lastGhostStartTick = firstSelectionTick;

        preMoveData = new Dictionary<int, SortedDictionary<int, T>>
        {
            { 0, laneSet.ExportData() }
        };
        originalMovingDataSet = new Dictionary<int, SortedDictionary<int, T>>
        {
            { 0,  selection.ExportNormalizedData() }
        };

        foreach (var tick in laneSet.protectedTicks)
        {
            if (originalMovingDataSet[0].ContainsKey(tick - firstSelectionTick))
            {
                originalMovingDataSet[0].Remove(tick - firstSelectionTick);
            }
        }

        foreach (var tick in originalMovingDataSet[0].Keys)
        {
            preMoveData[0].Remove(tick + firstSelectionTick, out T data);
        }

        inProgress = true;
    }


    public UniversalMoveData()
    {
        inProgress = false;
        lastMouseTick = -1;
        lastLane = int.MinValue;
    }

    public Dictionary<int, SortedDictionary<int, T>> OneDGetMoveData()
    {
        var boundsCorrectedData = MakeEmptyDataSet();

        foreach (var movingLane in originalMovingDataSet)
        {
            var laneID = movingLane.Key;

            var data = new SortedDictionary<int, T>(originalMovingDataSet[laneID]);
            boundsCorrectedData[laneID] = data;

            foreach (var item in new SortedDictionary<int, T>(data))
            {
                if (item.Key + lastGhostStartTick < 0)
                {
                    boundsCorrectedData[laneID].Remove(item.Key);
                    boundsCorrectedData[laneID][0] = item.Value;
                    continue;
                }

                if (item.Key + lastGhostStartTick > SongTime.SongLengthTicks)
                {
                    boundsCorrectedData[laneID].Remove(item.Key);
                    boundsCorrectedData[laneID][SongTime.SongLengthTicks] = item.Value;
                    continue;
                }
            }
        }

        return boundsCorrectedData;
    }

    public Dictionary<int, SortedDictionary<int, T>> GetMoveData(int laneShift)
    {
        var boundsCorrectedData = OneDGetMoveData();

        if (laneShift == 0) return boundsCorrectedData;

        var laneIDs = originalMovingDataSet.Keys.ToList();
        laneIDs.Sort();

        var laneSmooshOutput = MakeEmptyDataSet();
        foreach (var id in laneIDs)
        {
            int targetLaneID = id + laneShift;

            // This loop is structured this way so that there is no data
            // loss when users decide to shift the lane of a selection
            // If data is marked for destruction (for example, if the original lane
            // was orange, and laneShift = +1, orange would be deleted (as it is the highest lane in this context))
            // then instead of destroying it, tell the LINQ call below to forward the data to either
            // the lowest or highest dictionary in the output.
            if (laneShift < 0 && id < Mathf.Abs(laneShift))
            {
                targetLaneID = 0;
            }
            else if (laneShift > 0 && targetLaneID >= boundsCorrectedData.Count)
            {
                targetLaneID = boundsCorrectedData.Count - 1;
            }

            // taken from https://stackoverflow.com/questions/294138/merging-dictionaries-in-c-sharp (second answer)
            boundsCorrectedData[id].ToList().ForEach(item => laneSmooshOutput[targetLaneID][item.Key] = item.Value);
        }

        return laneSmooshOutput;
    }

    public Dictionary<int, SortedDictionary<int, T>> GetOriginalDataSet()
    {
        var output = MakeEmptyDataSet();
        foreach (var lane in preMoveData)
        {
            output[lane.Key] = lane.Value;

            foreach (var tick in originalMovingDataSet[lane.Key].Keys)
            {
                output[lane.Key].Add(tick + firstSelectionTick, originalMovingDataSet[lane.Key][tick]);
            }
        }

        return output;
    }

    Dictionary<int, SortedDictionary<int, T>> MakeEmptyDataSet()
    {
        Dictionary<int, SortedDictionary<int, T>> outputSet = new();
        foreach (var set in originalMovingDataSet)
        {
            outputSet[set.Key] = new();
        }
        return outputSet;
    }
}
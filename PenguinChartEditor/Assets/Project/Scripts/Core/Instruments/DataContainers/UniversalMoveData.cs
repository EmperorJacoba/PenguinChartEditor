using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Overlays;
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
        Lanes<T> laneData
        )
    {
        firstMouseTick = currentMouseTick;
        lastMouseTick = currentMouseTick;
        firstLane = currentLane;

        firstSelectionTick = laneData.GetFirstSelectionTick();
        if (firstSelectionTick == SelectionSet<T>.NONE_SELECTED) return;

        lastGhostStartTick = firstSelectionTick;

        // preMoveData needs selection data removed
        preMoveData = laneData.ExportData();
        originalMovingDataSet = laneData.ExportNormalizedSelection();

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
            { 0, selection.ExportNormalizedData() }
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

    // This uses a linked list to make moving across highways in starpower mode versitile (movable highway positioning)
    public Dictionary<int, SortedDictionary<int, T>> GetMoveData(int laneShift, LinkedList<int> laneOrdering)
    {
        var boundsCorrectedData = OneDGetMoveData();
        if (laneShift == 0) return boundsCorrectedData;

        var laneSmooshOutput = MakeEmptyDataSet();

        if (laneShift < 0)
        {
            LinkedListNode<int> activeNode = laneOrdering.Last;

            while (activeNode != null)
            {
                LinkedListNode<int> targetNode = activeNode;
                for (int i = 0; i > laneShift; i--)
                {
                    if (targetNode.Previous != null)
                    {
                        targetNode = targetNode.Previous;
                    }
                    else break;
                }
                boundsCorrectedData[activeNode.Value].ToList().ForEach(item => laneSmooshOutput[targetNode.Value][item.Key] = item.Value);

                activeNode = activeNode.Previous;
            }
        }
        else
        {
            LinkedListNode<int> activeNode = laneOrdering.First;

            while (activeNode != null)
            {
                LinkedListNode<int> targetNode = activeNode;

                for (int i = 0; i < laneShift; i++)
                {
                    if (targetNode.Next != null)
                    {
                        targetNode = targetNode.Next;
                    }
                    else break;
                }
                boundsCorrectedData[activeNode.Value].ToList().ForEach(item => laneSmooshOutput[targetNode.Value][item.Key] = item.Value);

                activeNode = activeNode.Next;
            }
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

    public MinMaxTicks GetChangedDataRange(LinkedList<int> laneOrdering)
    {
        var movingDataSet = GetMoveData(lastLane - firstLane, laneOrdering);

        int startValidationTick = int.MaxValue;
        int endValidationTick = -1;

        for (int i = 0; i < movingDataSet.Count; i++)
        {
            if (movingDataSet[i].Count > 0)
            {
                endValidationTick = Mathf.Max(movingDataSet[i].Keys.Max() + lastGhostStartTick, endValidationTick);
                startValidationTick = Mathf.Min(movingDataSet[i].Keys.Min() + lastGhostStartTick, startValidationTick);
            }
        }

        return new(startValidationTick, endValidationTick);
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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveDataPackage<T> where T : IEventData
{
    public bool inProgress = false;

    public int firstMouseTick;
    public int lastMouseTick;
    public int firstSelectionTick;

    SortedDictionary<int, T>[] originalMovingDataSet;
    public SortedDictionary<int, T>[] GetMoveData(int laneShift) 
    {
        if (laneShift == 0) return originalMovingDataSet;

        // soooooo open note data is in the last array position
        // even though in THEORY open notes should have a lower pitch
        // than green, so put open note in the lowest position
        // for correct moving between lanes via pitch
        // this is done because in every other part of Penguin it makes
        // vastly more sense for green to be 0 and not open,
        // because in a .chart file N 0 0 means a green note (makes parsing/exporting more intuitive)
        SortedDictionary<int, T>[] sequentialMoveData = new SortedDictionary<int, T>[originalMovingDataSet.Length];

        if (Chart.instance.SceneDetails.currentScene == SceneType.fiveFretChart)
        {
            sequentialMoveData[0] = originalMovingDataSet[^1];
            for (int i = 1; i < sequentialMoveData.Length; i++)
            {
                sequentialMoveData[i] = originalMovingDataSet[i - 1];
            }
        }
        else sequentialMoveData = originalMovingDataSet;

        SortedDictionary<int, T>[] output = new SortedDictionary<int, T>[originalMovingDataSet.Length];

        for (int i = 0; i < output.Length; i++)
        {
            // this loop is here to make sure no dicts end up as
            // null (which happens due to some lanes getting skipped
            // by a lane shift, which will cause errors down the line)
            // this cannot be implicitly done in the loop below
            // because that loop is based on the cached move data,
            // not the output data (to allow for lane "smooshing" (I have no better word to describe this))
            output[i] = new();
        }

        for (int i = 0; i < sequentialMoveData.Length; i++)
        {
            int outputTargetIndex = i + laneShift;

            // This loop is structured this way so that there is no data
            // loss when users decide to shift the lane of a selection
            // If data is marked for destruction (for example, if the original lane
            // was orange, and laneShift = +1, orange would be deleted (as it is the highest lane in this context))
            // then instead of destroying it, tell the LINQ call below to forward the data to either
            // the lowest or highest dictionary in the output.
            if (laneShift < 0 && i < Mathf.Abs(laneShift))
            {
                outputTargetIndex = 0;
            }
            else if (laneShift > 0 && outputTargetIndex >= output.Length)
            {
                outputTargetIndex = output.Length - 1;
            }

            // taken from https://stackoverflow.com/questions/294138/merging-dictionaries-in-c-sharp (second answer)
            sequentialMoveData[i].ToList().ForEach(item => output[outputTargetIndex][item.Key] = item.Value);
        }

        // undo the lane shift in the very first loop to export correct data
        SortedDictionary<int, T>[] correctedOutput = new SortedDictionary<int, T>[originalMovingDataSet.Length];
        if (Chart.instance.SceneDetails.currentScene == SceneType.fiveFretChart)
        {
            correctedOutput[^1] = output[0];
            for (int i = 0; i < correctedOutput.Length - 1; i++)
            {
                correctedOutput[i] = output[i + 1];
            }
        }
        else correctedOutput = sequentialMoveData;

        return correctedOutput;
    }
    public readonly SortedDictionary<int, T>[] preMoveData;
    public int lastGhostStartTick;
    public int lastGhostEndTick
    {
        get
        {
            HashSet<int> maxTicks = new();
            foreach (var lane in originalMovingDataSet)
            {
                if (lane.Count > 0) maxTicks.Add(lane.Keys.Max());
            }
            return lastGhostStartTick + maxTicks.Max();
        }
    }

    public readonly int firstLane;
    public int lastLane;

    public MoveDataPackage(
        int currentMouseTick, 
        int firstLane,
        SortedDictionary<int, T>[] laneData, 
        SortedDictionary<int, T>[] selectionData, 
        int firstSelectionTick
        )
    {
        firstMouseTick = currentMouseTick;
        lastMouseTick = currentMouseTick;
        this.firstLane = firstLane;

        this.firstSelectionTick = firstSelectionTick;
        if (firstSelectionTick == SelectionSet<T>.NONE_SELECTED)
        {
            return;
        }
        lastGhostStartTick = firstSelectionTick;

        // preMoveData needs selection data removed
        preMoveData = laneData;
        originalMovingDataSet = selectionData;

        for (int i = 0; i < preMoveData.Length; i++)
        {
            foreach (var tick in originalMovingDataSet[i].Keys)
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
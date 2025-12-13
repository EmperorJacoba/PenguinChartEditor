using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Two dimensions in this scenario meaning across time and lanes. Enables lane-to-lane movement.
public class TwoDimensionalMoveData<T> where T : IEventData
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
    readonly SortedDictionary<int, T>[] originalMovingDataSet;

    /// <summary>
    /// Contains the original chart data with the data being moved deleted.
    /// </summary>
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

    public TwoDimensionalMoveData(
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
        if (firstSelectionTick == SelectionSet<T>.NONE_SELECTED) return;

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

    public TwoDimensionalMoveData()
    {
        inProgress = false;
        lastMouseTick = -1;
        lastLane = int.MinValue;
    }

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

        SortedDictionary<int, T>[] pitchWrappedOutput = new SortedDictionary<int, T>[originalMovingDataSet.Length];

        for (int i = 0; i < pitchWrappedOutput.Length; i++)
        {
            // this loop is here to make sure no dicts end up as
            // null (which happens due to some lanes getting skipped
            // by a lane shift, which will cause errors down the line)
            // this cannot be implicitly done in the loop below
            // because that loop is based on the cached move data,
            // not the output data (to allow for lane "smooshing" (I have no better word to describe this))
            pitchWrappedOutput[i] = new();
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
            else if (laneShift > 0 && outputTargetIndex >= pitchWrappedOutput.Length)
            {
                outputTargetIndex = pitchWrappedOutput.Length - 1;
            }

            // taken from https://stackoverflow.com/questions/294138/merging-dictionaries-in-c-sharp (second answer)
            sequentialMoveData[i].ToList().ForEach(item => pitchWrappedOutput[outputTargetIndex][item.Key] = item.Value);
        }

        // undo the lane shift in the very first loop to export correct data
        SortedDictionary<int, T>[] correctedOutput = new SortedDictionary<int, T>[originalMovingDataSet.Length];
        if (Chart.instance.SceneDetails.currentScene == SceneType.fiveFretChart)
        {
            correctedOutput[^1] = pitchWrappedOutput[0];
            for (int i = 0; i < correctedOutput.Length - 1; i++)
            {
                correctedOutput[i] = pitchWrappedOutput[i + 1];
            }
        }
        else correctedOutput = pitchWrappedOutput;

        return correctedOutput;
    }

    public SortedDictionary<int, T>[] GetOriginalDataSet()
    {
        SortedDictionary<int, T>[] output = new SortedDictionary<int, T>[preMoveData.Length];
        for (int i = 0; i < preMoveData.Length; i++)
        {
            output[i] = preMoveData[i];
        }

        for (int i = 0; i < output.Length; i++)
        {
            foreach (var tick in originalMovingDataSet[i].Keys)
            {
                output[i].Add(tick + firstSelectionTick, originalMovingDataSet[i][tick]);
            }
        }
        return output;
    }
}
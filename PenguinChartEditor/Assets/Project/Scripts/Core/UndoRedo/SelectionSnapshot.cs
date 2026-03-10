using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

public interface ISelectionSnapshot
{
    public ISelectionSnapshot ScaleSelectionSnapshot(int offset);
    public ISelectionSnapshot ShiftSnapshotLanes(int laneShift, LinkedList<int> laneProgression);
}

public class SelectionSnapshot<T> : ISelectionSnapshot where T : IEventData
{
    public readonly Dictionary<int, SortedDictionary<int, T>> savedSelectionData;

    public SelectionSnapshot(Lanes<T> laneController)
    {
        savedSelectionData = laneController.ExportSelectionData();
    }

    public SelectionSnapshot(Dictionary<int, SortedDictionary<int, T>> poppedData)
    {
        savedSelectionData = poppedData;
    }

    public ISelectionSnapshot ScaleSelectionSnapshot(int offset)
    {
        var adjustedDict = new Dictionary<int, SortedDictionary<int, T>>();

        foreach (var laneID in savedSelectionData.Select(x => x.Key))
        {
            adjustedDict[laneID] =
                SelectionSnapshotTransformationTools.ScaleDataWithBoundsCorrection(savedSelectionData[laneID], offset);
        }

        return new SelectionSnapshot<T>(adjustedDict);
    }

    public ISelectionSnapshot ShiftSnapshotLanes(int laneShift, LinkedList<int> laneProgression)
    {
        var adjustedDict = SelectionSnapshotTransformationTools.MakeEmptyDataSet(savedSelectionData);
        
        if (laneShift < 0)
        {
            LinkedListNode<int> activeNode = laneProgression.Last;

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
                savedSelectionData[activeNode.Value].ToList().ForEach(item => adjustedDict[targetNode.Value][item.Key] = item.Value);

                activeNode = activeNode.Previous;
            }
        }
        else
        {
            LinkedListNode<int> activeNode = laneProgression.First;

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
                savedSelectionData[activeNode.Value].ToList().ForEach(item => adjustedDict[targetNode.Value][item.Key] = item.Value);
                
                activeNode = activeNode.Next;
            }
        }

        return new SelectionSnapshot<T>(adjustedDict);
    }

    public override string ToString()
    {
        StringBuilder output = new();

        output.AppendLine($"SelectionSnapshot ({typeof(T)})");
        foreach (var lane in savedSelectionData)
        {
            output.AppendLine($"Lane: {lane.Key}:");
            output.AppendLine($"// ---- //\n");
            
            foreach (var item in lane.Value)
            {
                output.AppendLine($"\t{item.Key} = {item.Value}");
            }

            output.AppendLine($"\n// ---- //\n");
        }

        return output.ToString();
    }
}

public class SyncTrackSelectionSnapshot : ISelectionSnapshot
{
    public readonly SortedDictionary<int, BPMData> bpmSelection;
    public readonly SortedDictionary<int, TSData> tsSelection;

    public SyncTrackSelectionSnapshot(SyncTrackLanes laneController)
    {
        bpmSelection = laneController.bpmSelection.ExportData();
        tsSelection = laneController.tsSelection.ExportData();
    }

    public SyncTrackSelectionSnapshot(SortedDictionary<int, BPMData> bpmSnap, SortedDictionary<int, TSData> tsSnap)
    {
        bpmSelection = bpmSnap;
        tsSelection = tsSnap;
    }

    public ISelectionSnapshot ScaleSelectionSnapshot(int offset)
    {
        var bpmScaled = SelectionSnapshotTransformationTools.ScaleDataWithBoundsCorrection(bpmSelection, offset);
        var tsScaled = SelectionSnapshotTransformationTools.ScaleDataWithBoundsCorrection(tsSelection, offset);

        return new SyncTrackSelectionSnapshot(bpmScaled, tsScaled);
    }

    public ISelectionSnapshot ShiftSnapshotLanes(int laneShift, LinkedList<int> laneProgression)
    {
        throw new System.NotImplementedException("SyncTrack does not support cross-lane transformations.");
    }
}

internal static class SelectionSnapshotTransformationTools
{
    public static SortedDictionary<int, T> ScaleDataWithBoundsCorrection<T>(SortedDictionary<int, T> originalData, int offset) where T : IEventData
    {
        var boundsCorrectedData = new SortedDictionary<int, T>();

        foreach (var item in originalData)
        {
            var targetMoveTick = item.Key + offset;
            if (targetMoveTick < 0)
            {
                boundsCorrectedData[0] = item.Value;
                continue;
            }

            if (targetMoveTick > SongTime.SongLengthTicks)
            {
                boundsCorrectedData[SongTime.SongLengthTicks] = item.Value;
                continue;
            }

            boundsCorrectedData[targetMoveTick] = item.Value;
        }

        return boundsCorrectedData;
    }
    
    public static Dictionary<int, SortedDictionary<int, T>> MakeEmptyDataSet<T>(Dictionary<int, SortedDictionary<int, T>> targetType)
    {
        Dictionary<int, SortedDictionary<int, T>> outputSet = new();
        foreach (var set in targetType)
        {
            outputSet[set.Key] = new SortedDictionary<int, T>();
        }
        return outputSet;
    }
}
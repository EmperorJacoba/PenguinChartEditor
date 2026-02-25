using System.Collections.Generic;

public interface ISelectionSnapshot {}

public class SelectionSnapshot<T> : ISelectionSnapshot where T : IEventData
{
    public readonly Dictionary<int, SortedDictionary<int, T>> savedSelectionData;

    public SelectionSnapshot(Lanes<T> laneController)
    {
        savedSelectionData = laneController.ExportSelectionData();
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
}
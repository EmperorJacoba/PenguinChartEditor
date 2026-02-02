using System.Collections.Generic;
using System;
using System.Linq;

public class SectionInstrument : IInstrument
{
    public InstrumentType InstrumentName { get; set; } = InstrumentType.events;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;
    public HeaderType InstrumentID { get; } = HeaderType.Events;

    public LaneSet<SectionData> GetLaneData() => laneData;
    private LaneSet<SectionData> laneData;

    public SelectionSet<SectionData> GetLaneSelection() => selection;
    private SelectionSet<SectionData> selection;
    public List<string> ExportAllEvents()
    {
        throw new System.NotImplementedException();
    }

    public void ClearAllSelections() => selection.Clear();
    public bool NoteSelectionContains(int tick, int lane) => selection.Contains(tick);
    public int NoteSelectionCount => selection.Count();
    public void ShiftClickSelectLane(int start, int end, int lane) => selection.ShiftClickSelectInRange(start, end);
    public void ShiftClickSelect(int start, int end) => selection.ShiftClickSelectInRange(start, end);
    public void ShiftClickSelect(int tick) => selection.ShiftClickSelectInRange(tick, tick);
    public void ClearTickFromAllSelections(int tick) => selection.Remove(tick);
    public void DeleteTicksInSelection() => selection.PopSelectedTicksFromLane();
    public void DeleteTickInLane(int tick, int lane) => laneData.Remove(tick);
    public void DeleteAllEventsAtTick(int tick) => laneData.Remove(tick);
    public List<int> GetUniqueTickSet() => laneData.Keys.ToList();
    public bool IsNoteSelectionEmpty() => selection.Count == 0;

    public void SetUpInputMap()
    {
        throw new System.NotImplementedException();
    }

    public string ConvertSelectionToString()
    {
        throw new System.NotImplementedException();
    }

    public void AddChartFormattedEventsToInstrument(string clipboardData, int offset)
    {
        throw new System.NotImplementedException();
    }

    public ISelection GetLaneSelection(int lane) => selection;

    #region Not Implemented

    public SoloDataSet SoloData
    {
        get { throw new NotImplementedException("Sections do not have solo events. If you are using the SoloEvent suite, it is not required."); }
        set { throw new NotImplementedException("Sections do not have solo events. If you are using the SoloEvent suite, it is not required."); }
    }
    public ILaneData GetBarLaneData() => throw new System.NotImplementedException("No bar lane in sections");
    public ILaneData GetLaneData(int lane) => laneData;

    #endregion
}
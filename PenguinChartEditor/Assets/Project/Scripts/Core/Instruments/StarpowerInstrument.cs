

using System;
using System.Collections.Generic;

public class StarpowerInstrument : IInstrument
{
    private Lanes<StarpowerEventData> Lanes;
    public SoloDataSet SoloData
    {
        get { throw new NotImplementedException("Starpower does not have solo events. If you are using the SoloEvent suite, it is not required."); }
        set { throw new NotImplementedException("Starpower does not have solo events. If you are using the SoloEvent suite, it is not required."); }
    }
    public InstrumentType InstrumentName { get; set; } = InstrumentType.starpower;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;

    public int NoteSelectionCount => Lanes.GetTotalSelectionCount();

    public List<int> UniqueTicks => Lanes.UniqueTicks;

    public void AddChartFormattedEventsToInstrument(string lines, int offset)
    {
        throw new System.NotImplementedException();
    }

    public void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines)
    {
        throw new System.NotImplementedException();
    }

    public void ClearAllSelections() => Lanes.ClearAllSelections();

    public string ConvertSelectionToString()
    {
        throw new System.NotImplementedException();
    }

    public void DeleteAllEventsAtTick(int tick) => Lanes.PopAllEventsAtTick(tick);

    public void DeleteTickInLane(int tick, int lane) => Lanes.PopTickFromLane(tick, lane);

    public void DeleteTicksInSelection() => Lanes.DeleteAllTicksInSelection();

    public List<string> ExportAllEvents()
    {
        throw new System.NotImplementedException();
    }

    public bool NoteSelectionContains(int tick, int lane) => Lanes.GetLaneSelection(lane).Contains(tick);

    public void ClearTickFromAllSelections(int tick) => Lanes.ClearTickFromAllSelections(tick);

    public void SetUpInputMap() { }

    public void ShiftClickSelect(int start, int end) => Lanes.ShiftClickSelect(start, end);

    public void ShiftClickSelect(int tick) => Lanes.ShiftClickSelect(tick, tick);
}
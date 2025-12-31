

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

    public List<int> UniqueTicks => Lanes.GetUniqueTickSet();

    public StarpowerInstrument(List<KeyValuePair<int, string>> starpowerEvents)
    {
        int instrumentCount = 0;
        foreach (var instrumentType in Enum.GetValues(typeof(HeaderType)))
        {
            // instruments begin at 10^1. Refer to HeaderType for specifics.
            if ((int)instrumentType < 10) continue;
            instrumentCount++;
        }
        Lanes = new(instrumentCount);
    }

    public void AddChartFormattedEventsToInstrument(string lines, int offset)
    {
        throw new System.NotImplementedException();
        /*
        if (!int.TryParse(values[NOTE_IDENTIFIER_INDEX], out noteIdentifier))
        {
            Chart.Log($"Invalid special identifier for {InstrumentName} @ tick {uniqueTick}: {values[NOTE_IDENTIFIER_INDEX]}");
            break;
        }

        if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
        {
            Chart.Log($"Invalid sustain for {InstrumentName} @ tick {uniqueTick}: {values[SUSTAIN_INDEX]}");
            break;
        }

        if (noteIdentifier != STARPOWER_INDICATOR) break; // should only have starpower indicator, no fills or anything

        SpecialEvents[uniqueTick] = new SpecialData(sustain, SpecialData.EventType.starpower);

        break; 
        */
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

    public void DeleteAllEventsAtTick(int tick) 
    {
        Lanes.PopAllEventsAtTick(tick);
        Chart.Refresh();
    }


    public void DeleteTickInLane(int tick, int lane) 
    { 
        Lanes.PopTickFromLane(tick, lane);
        Chart.Refresh();
    }

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
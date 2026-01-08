using System;
using System.Collections.Generic;

public class StarpowerInstrument : IInstrument
{
    private const int EVENT_TYPE_IDENTIFIER_INDEX = 1;
    private const int SUSTAIN_INDEX = 2;

    /// <summary>
    /// Access instrument data with GetLane(int), where int is casted version of HeaderType, since each traditional instrument has its own set of starpower events.
    /// </summary>
    private Lanes<StarpowerEventData> Lanes;
    ILaneData IInstrument.GetLaneData(int lane) => Lanes.GetLane(lane);
    ILaneData IInstrument.GetBarLaneData()
    {
        throw new NotImplementedException($"Starpower does not have a bar lane. Please format the note receivers to access your intended instrument instead of the loaded instrument.");
    }
    ISelection IInstrument.GetLaneSelection(int lane) => Lanes.GetLaneSelection(lane);
    public SoloDataSet SoloData
    {
        get { throw new NotImplementedException("Starpower does not have solo events. If you are using the SoloEvent suite, it is not required."); }
        set { throw new NotImplementedException("Starpower does not have solo events. If you are using the SoloEvent suite, it is not required."); }
    }
    public InstrumentType InstrumentName { get; set; } = InstrumentType.starpower;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;

    public int NoteSelectionCount => Lanes.GetTotalSelectionCount();

    public List<int> UniqueTicks => Lanes.GetUniqueTickSet();

    void SetUpLanes()
    {
        List<int> headerTypeIDs = new();
        foreach (var instrumentType in Enum.GetValues(typeof(HeaderType)))
        {
            // instruments begin at 10^1. Refer to HeaderType for specifics.
            if ((int)instrumentType < 10) continue;
            headerTypeIDs.Add((int)instrumentType);
        }
        Lanes = new(headerTypeIDs);
    }

    public StarpowerInstrument(List<RawStarpowerEvent> starpowerEvents)
    {
        SetUpLanes();
        ParseRawStarpowerEvents(starpowerEvents);
    }

    void ParseRawStarpowerEvents(List<RawStarpowerEvent> starpowerEvents)
    {
        foreach (var @event in starpowerEvents)
        {
            var data = @event.data.Split(" ");

            // S identifier should already be checked by ChartParser

            var fill = data[EVENT_TYPE_IDENTIFIER_INDEX] == "64";

            if (!int.TryParse(data[SUSTAIN_INDEX], out int sustain))
            {
                throw new ArgumentException($"Invalid sustain @ tick {@event.tick} for instrument {@event.header}. Expected integer, given {data[2]}.");
            }
            StarpowerEventData parsedData = new(fill, sustain);

            Lanes.GetLane((int)@event.header).Add(@event.tick, parsedData);
        }
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
        Chart.InPlaceRefresh();
    }


    public void DeleteTickInLane(int tick, int lane) 
    { 
        Lanes.PopTickFromLane(tick, lane);
        Chart.InPlaceRefresh();
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
using System;
using System.Collections.Generic;

public class FourLaneDrumInstrument : IInstrument
{
    public Lanes<FourLaneDrumNoteData> Lanes;
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }

    public int TotalSelectionCount => throw new System.NotImplementedException();
    public List<int> UniqueTicks => Lanes.UniqueTicks;


    public enum LaneOrientation
    {
        red = 0,
        yellow = 1,
        blue = 2,
        green = 3,
        kick = 4
    }
    public List<string> ExportAllEvents()
    {
        throw new System.Exception();
    }

    public void ClearAllSelections()
    {
        throw new System.NotImplementedException();
    }

    public void ShiftClickSelect(int start, int end)
    {
        throw new System.NotImplementedException();
    }

    public void ShiftClickSelect(int tick)
    {
        throw new System.NotImplementedException();
    }

    public void ShiftClickSelect(int tick, bool temporary)
    {
        throw new System.NotImplementedException();
    }

    public void ReleaseTemporaryTicks()
    {
        throw new System.NotImplementedException();
    }

    public void RemoveTickFromAllSelections(int tick)
    {
        throw new System.NotImplementedException();
    }

    public void AddChartFormattedEventsToInstrument(string lines, int offset)
    {
        throw new NotImplementedException();
    }

    public void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines)
    {
        throw new NotImplementedException();
    }

    public void ToggleTap() { }
    public void ToggleForced() { }
    public void SetUpInputMap() { }

    public string ConvertSelectionToString()
    {
        throw new NotImplementedException();
    }

    public void DeleteTicksInSelection()
    {
        throw new NotImplementedException();
    }

    public bool justMoved { get; set; }
}
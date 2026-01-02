using System;
using System.Collections.Generic;

/*
public class GHLInstrument : IInstrument
{
    public Lanes<GHLNoteData> Lanes;
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SoloDataSet SoloData { get; set; }


    public InstrumentType InstrumentName { get; set; }
    public DifficultyType Difficulty { get; set; }
    public List<int> UniqueTicks => Lanes.GetUniqueTickSet();


    public int NoteSelectionCount => throw new System.NotImplementedException();

    public bool justMoved { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public enum LaneOrientation
    {
        white1 = 0,
        white2 = 1,
        white3 = 2,
        black1 = 3,
        black2 = 4,
        black3 = 5,
        open = 6
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

    public void ClearTickFromAllSelections(int tick)
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

    public void DeleteTickInLane(int tick, int lane)
    {
        throw new NotImplementedException();
    }

    public bool NoteSelectionContains(int tick, int lane)
    {
        throw new NotImplementedException();
    }

    public void DeleteAllEventsAtTick(int tick)
    {
        throw new NotImplementedException();
    }
} */
using UnityEngine;
using System.Collections.Generic;

public interface IInstrument
{
    SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
}

public class FiveFretInstrument : IInstrument
{
    public SortedDictionary<int, FiveFretNoteData>[] Lanes { get; set; }
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

    public enum LaneOrientation
    {
        green = 0,
        red = 1,
        yellow = 2,
        blue = 3,
        orange = 4,
        open = 5
    }

    public FiveFretInstrument(
        SortedDictionary<int, FiveFretNoteData>[] lanes,
        SortedDictionary<int, SpecialData> starpower,
        SortedDictionary<int, LocalEventData> localEvents)
    {
        Lanes = lanes;
        SpecialEvents = starpower;
        LocalEvents = localEvents;  
    }
}

public class FourLaneDrumInstrument : IInstrument
{
    public SortedDictionary<int, FourLaneDrumNoteData>[] Lanes { get; set; }
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

    public enum LaneOrientation
    {
        red = 0,
        yellow = 1,
        blue = 2,
        green = 3,
        kick = 4
    }
}

public class GHLInstrument : IInstrument
{
    public SortedDictionary<int, GHLNoteData>[] Lanes { get; set; }
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

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
}

/*
public class VoxInstrument : IInstrument
{
    // not yet implemented
} */
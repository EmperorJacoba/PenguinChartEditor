using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// no <T> definition because otherwise IInstrument collections do not work properly
public interface IInstrument
{
    SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    InstrumentType Instrument { get; set; }
    DifficultyType Difficulty { get; set; }
    List<string> ExportAllEvents();
}

public class FiveFretInstrument : IInstrument
{
    public SortedDictionary<int, FiveFretNoteData>[] Lanes { get; set; }
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }

    /// <summary>
    /// Corresponds to this lane's position in Lanes[].
    /// </summary>
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
        SortedDictionary<int, LocalEventData> localEvents,
        InstrumentType instrument,
        DifficultyType difficulty
        )
    {
        Lanes = lanes;
        SpecialEvents = starpower;
        LocalEvents = localEvents;
        Instrument = instrument;
        Difficulty = difficulty;
    }

    // currently only supports N events, need support for E and S
    // also needs logic for when and where to place forced/tap identifiers (data in struct is not enough)
    public List<string> ExportAllEvents()
    {
        List<string> notes = new();
        for (int i = 0; i < Lanes.Length; i++)
        {
            int laneIdentifier = i != 5 ? i : 7;

            foreach (var note in Lanes[i])
            {
                string value = $"\t{note.Key} = N {laneIdentifier} {note.Value.Sustain}";
                notes.Add(value);
            }
        }



        var orderedStrings = notes.OrderBy(i => int.Parse(i.Split(" = ")[0])).ToList();
        return orderedStrings;
    }
}

public class FourLaneDrumInstrument : IInstrument
{
    public SortedDictionary<int, FourLaneDrumNoteData>[] Lanes { get; set; }
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }
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
}

public class GHLInstrument : IInstrument
{
    public SortedDictionary<int, GHLNoteData>[] Lanes { get; set; }
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }

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
}

/*
public class VoxInstrument : IInstrument
{
    // not yet implemented
} */
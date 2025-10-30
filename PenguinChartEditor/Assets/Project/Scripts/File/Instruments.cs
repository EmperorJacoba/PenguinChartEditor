using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

// please please please do not add <T> to this
// i have done this like 7 times and it makes chartparser really ugly
// future me: PLEASE STOP ADDING <T>! IT WILL NOT WORK THIS TIME! LIKE THE 7 OTHER TIMES
public interface IInstrument
{
    SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    InstrumentType Instrument { get; set; }
    DifficultyType Difficulty { get; set; }
    List<string> ExportAllEvents();

    void ClearAllSelections();
    int TotalSelectionCount { get; }
    public void ShiftClickSelect(int start, int end);
    public void ShiftClickSelect(int tick);
}

// used for 
public class SyncTrackInstrument : IInstrument
{
    // Lanes located in respective libraries
    // This class is pretty much for shift click and clearing both TS and BPMData selections when needed
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

    public EventData<BPMData> bpmEventData = new();
    public EventData<TSData> tsEventData = new();

    public MoveData<BPMData> bpmMoveData = new();
    public MoveData<TSData> tsMoveData = new();

    public InstrumentType Instrument { get; set; } = InstrumentType.synctrack;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;

    public int TotalSelectionCount
    {
        get
        {
            return bpmEventData.Selection.Count + tsEventData.Selection.Count;
        }
    }

    public void ClearAllSelections()
    {
        bpmEventData.Selection.Clear();
        tsEventData.Selection.Clear();
    }

    public void ShiftClickSelect(int start, int end)
    {
        bpmEventData.Selection.Clear();
        tsEventData.Selection.Clear();

        var bpmSelection = Tempo.Events.Keys.ToList().Where(x => x >= start && x <= end);
        foreach (var tick in bpmSelection)
        {
            bpmEventData.Selection.Add(tick, Tempo.Events[tick]);
        }

        var tsSelection = TimeSignature.Events.Keys.ToList().Where(x => x >= start && x <= end);
        foreach (var tick in tsSelection)
        {
            tsEventData.Selection.Add(tick, TimeSignature.Events[tick]);
        }
    }

    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);
    public List<string> ExportAllEvents()
    {
        throw new System.NotImplementedException();
        // maybe use this instead of individual libraries in future?
    }
}

public class FiveFretInstrument : IInstrument
{
    public SortedDictionary<int, FiveFretNoteData>[] Lanes { get; set; }
    public EventData<FiveFretNoteData>[] InstrumentEventData { get; set; } = 
        new EventData<FiveFretNoteData>[6] { new (), new(), new(), new(), new(), new() };

    public MoveData<FiveFretNoteData>[] InstrumentMoveData { get; set; } =
        new MoveData<FiveFretNoteData>[6] { new(), new(), new(), new(), new(), new() };

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

    public int TotalSelectionCount 
    { 
        get
        {
            var sum = 0;
            foreach(var eventData in InstrumentEventData)
            {
                sum += eventData.Selection.Count;
            }
            return sum;
        } 
    }

    public void ClearAllSelections()
    {
        foreach (var eventData in InstrumentEventData)
        {
            eventData.Selection.Clear();
        }
    }

    public void ShiftClickSelect(int start, int end)
    {
        for (int i = 0; i < InstrumentEventData.Length; i++)
        {
            var selectionSet = InstrumentEventData[i].Selection;
            selectionSet.Clear();

            var selection = Lanes[i].Keys.ToList().Where(x => x >= start && x <= end);
            foreach (var tick in selection)
            {
                selectionSet.Add(tick, Lanes[i][tick]);
            }
        }
    }
    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);

    public void ShiftClickSustainClamp(int tick, int tickLength)
    {
        for (int i = 0; i < Lanes.Length; i++)
        {
            if (Lanes[i].ContainsKey(tick))
            {
                Lanes[i][tick] = new(tickLength, Lanes[i][tick].Flag, Lanes[i][tick].Default);
            }
        }
    }

    // currently only supports N events, need support for E and S
    // also needs logic for when and where to place forced/tap identifiers (data in struct is not enough - flag is LITERAL value, forced is the toggle between default and not behavior)
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
    public EventData<FourLaneDrumNoteData>[] InstrumentEventData { get; set; } =
    new EventData<FourLaneDrumNoteData>[6] { new(), new(), new(), new(), new(), new() };

    public MoveData<FourLaneDrumNoteData>[] InstrumentMoveData { get; set; } =
        new MoveData<FourLaneDrumNoteData>[6] { new(), new(), new(), new(), new(), new() };
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lane">The integer value of LaneOrientation. Use a cast!</param>
    /// <returns></returns>
    public SortedDictionary<int, FourLaneDrumNoteData> GetLaneData(int lane)
    {
        return Lanes[lane];
    }

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

    public int TotalSelectionCount
    {
        get
        {
            var sum = 0;
            foreach (var eventData in InstrumentEventData)
            {
                sum += eventData.Selection.Count;
            }
            return sum;
        }
    }

    public void ClearAllSelections()
    {
        foreach (var eventData in InstrumentEventData)
        {
            eventData.Selection.Clear();
        }
    }

    public void ShiftClickSelect(int start, int end)
    {
        for (int i = 0; i < InstrumentEventData.Length; i++)
        {
            var selectionSet = InstrumentEventData[i].Selection;
            selectionSet.Clear();

            var selection = Lanes[i].Keys.ToList().Where(x => x >= start && x <= end);
            foreach (var tick in selection)
            {
                selectionSet.Add(tick, Lanes[i][tick]);
            }
        }
    }
    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);
}

public class GHLInstrument : IInstrument
{
    public SortedDictionary<int, GHLNoteData>[] Lanes { get; set; }
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    public EventData<GHLNoteData>[] InstrumentEventData { get; set; } =
        new EventData<GHLNoteData>[6] { new(), new(), new(), new(), new(), new() };

    public MoveData<GHLNoteData>[] InstrumentMoveData { get; set; } =
        new MoveData<GHLNoteData>[6] { new(), new(), new(), new(), new(), new() };
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lane">The integer value of LaneOrientation. Use a cast!</param>
    /// <returns></returns>
    public SortedDictionary<int, GHLNoteData> GetLaneData(int lane)
    {
        return Lanes[lane];
    }

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

    public int TotalSelectionCount
    {
        get
        {
            var sum = 0;
            foreach (var eventData in InstrumentEventData)
            {
                sum += eventData.Selection.Count;
            }
            return sum;
        }
    }

    public void ClearAllSelections()
    {
        foreach (var eventData in InstrumentEventData)
        {
            eventData.Selection.Clear();
        }
    }

    public void ShiftClickSelect(int start, int end)
    {
        for (int i = 0; i < InstrumentEventData.Length; i++)
        {
            var selectionSet = InstrumentEventData[i].Selection;
            selectionSet.Clear();

            var selection = Lanes[i].Keys.ToList().Where(x => x >= start && x <= end);
            foreach (var tick in selection)
            {
                selectionSet.Add(tick, Lanes[i][tick]);
            }
        }
    }
    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);
}

/*
public class VoxInstrument : IInstrument
{
    // not yet implemented
} */
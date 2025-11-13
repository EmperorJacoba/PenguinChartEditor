using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public void ShiftClickSelect(int tick, bool temporary);
    public void ReleaseTemporaryTicks();
    public void RemoveTickFromAllSelections(int tick);
}

public class SyncTrackInstrument : IInstrument
{
    // Lanes located in respective libraries
    // This class is pretty much for shift click and clearing both TS and BPMData selections when needed
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

    public SelectionSet<BPMData> bpmSelection = new(Tempo.Events);
    public SelectionSet<TSData> tsSelection = new(TimeSignature.Events);

    public ClipboardSet<BPMData> bpmClipboard = new(Tempo.Events);
    public ClipboardSet<TSData> tsClipboard = new(TimeSignature.Events);

    public MoveData<BPMData> bpmMoveData = new();
    public MoveData<TSData> tsMoveData = new();

    public InstrumentType Instrument { get; set; } = InstrumentType.synctrack;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;

    public int TotalSelectionCount
    {
        get
        {
            return bpmSelection.Count + tsSelection.Count;
        }
    }

    public void ClearAllSelections()
    {
        bpmSelection.Clear();
        tsSelection.Clear();
    }

    public void ShiftClickSelect(int start, int end)
    {
        bpmSelection.Clear();
        tsSelection.Clear();

        bpmSelection.AddInRange(start, end);
        tsSelection.AddInRange(start, end);
    }

    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);
    public List<string> ExportAllEvents()
    {
        throw new System.NotImplementedException("Use export functions in Tempo and TimeSignature libraries.");
        // maybe use this instead of individual libraries in future?
    }

    public void ShiftClickSelect(int tick, bool temporary) => ShiftClickSelect(tick);
    public void ReleaseTemporaryTicks() { } // unneeded - no sustains lol
    public void RemoveTickFromAllSelections(int tick) 
    {
        bpmSelection.Remove(tick);
        tsSelection.Remove(tick);
    } // unneeded

}

public class FiveFretInstrument : IInstrument
{
    public Lanes<FiveFretNoteData> Lanes { get; set; }
    public MoveData<FiveFretNoteData>[] InstrumentMoveData { get; set; } =
        new MoveData<FiveFretNoteData>[6] { new(), new(), new(), new(), new(), new() };

    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }

    /// <summary>
    /// Corresponds to this lane's position in Lanes.
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
        Lanes<FiveFretNoteData> lanes,
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
            for (int i = 0; i < Lanes.Count; i++)
            {
                sum += Lanes.GetLaneSelection(i).Count;
            }
            return sum;
        } 
    }

    public void ClearAllSelections()
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            Lanes.GetLaneSelection(i).Clear();
        }
    }

    public void ShiftClickSelect(int start, int end)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            Lanes.GetLaneSelection(i).ShiftClickSelectInRange(start, end);
        }
    }
    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);

    public void ShiftClickSustainClamp(int tick, int tickLength)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            if (Lanes.GetLane(i).ContainsKey(tick))
            {
                Lanes.GetLane(i)[tick] = new(tickLength, Lanes.GetLane(i)[tick].Flag, Lanes.GetLane(i)[tick].Default);
            }
        }
    }
    public void ShiftClickSelect(int tick, bool temporary)
    {
        Lanes.TempSustainTicks.Add(tick);
        ShiftClickSelect(tick);
    }

    public void ReleaseTemporaryTicks()
    {
        Lanes.TempSustainTicks.Clear();
    }

    public void RemoveTickFromAllSelections(int tick)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            Lanes.GetLaneSelection(i).Remove(tick);
        }
    }

    // after an add
    // needs to update target tick if the last tick before current tick is within hopo range
    // needs to update next tick if the tick after current tick is within hopo range
    public void CheckForHopos(LaneOrientation lane, int changedTick)
    {
        if (Lanes.GetLane((int)lane).TryGetValue(changedTick, out var parameterLaneTickData))
        {
            if (!parameterLaneTickData.Default || 
                parameterLaneTickData.Flag == FiveFretNoteData.FlagType.tap) return;
        }

        void SetThisNoteHopo(LaneSet<FiveFretNoteData> activeLane) => activeLane[changedTick] = new(parameterLaneTickData.Sustain, FiveFretNoteData.FlagType.hopo);
        void SetThisNoteStrum(LaneSet<FiveFretNoteData> activeLane) => activeLane[changedTick] = new(parameterLaneTickData.Sustain, FiveFretNoteData.FlagType.strum);

        bool nextTickHopo = false;
        bool currentTickHopo = false;

        int previousTick = Lanes.GetPreviousTickEvent(changedTick);
        int nextTick = Lanes.GetNextTickEvent(changedTick);

        if (nextTick - changedTick < Chart.hopoCutoff) nextTickHopo = true;
        if (changedTick - previousTick < Chart.hopoCutoff) currentTickHopo = true;

        var activeLane = Lanes.GetLane((int)lane);
        if (currentTickHopo)
        {
            if (!Lanes.IsTickChord(changedTick) && !activeLane.Contains(previousTick))
            {
                SetThisNoteHopo(activeLane);
            }
            else
            {
                SetThisNoteStrum(activeLane);
            }
        }
        else
        {
            SetThisNoteStrum(activeLane);
        }

        if (nextTickHopo)
        {
            bool nextTickChord = Lanes.IsTickChord(nextTick);
            for (int i = 0; i < Lanes.Count; i++)
            {
                if (i == (int)lane) continue;

                activeLane = Lanes.GetLane(i);
                if (activeLane.TryGetValue(nextTick, out var iData))
                {
                    if (!iData.Default) continue;
                    if (iData.Flag == FiveFretNoteData.FlagType.tap) break;

                    if (nextTickChord)
                    {
                        activeLane[nextTick] = new(iData.Sustain, FiveFretNoteData.FlagType.strum);
                    }
                    else
                    {
                        activeLane[nextTick] = new(iData.Sustain, FiveFretNoteData.FlagType.hopo);
                    }
                }
            }
        }
    }

    // currently only supports N events, need support for E and S
    // also needs logic for when and where to place forced/tap identifiers (data in struct is not enough - flag is LITERAL value, forced is the toggle between default and not behavior)
    public List<string> ExportAllEvents()
    {
        List<string> notes = new();
        for (int i = 0; i < Lanes.Count; i++)
        {
            int laneIdentifier = i != 5 ? i : 7;

            foreach (var note in Lanes.GetLane(i))
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
    public Lanes<FourLaneDrumNoteData> Lanes;
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

    public MoveData<FourLaneDrumNoteData>[] InstrumentMoveData { get; set; } =
        new MoveData<FourLaneDrumNoteData>[6] { new(), new(), new(), new(), new(), new() };
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }

    public int TotalSelectionCount => throw new System.NotImplementedException();

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
}

public class GHLInstrument : IInstrument
{
    public Lanes<GHLNoteData> Lanes;
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

    public MoveData<GHLNoteData>[] InstrumentMoveData { get; set; } =
        new MoveData<GHLNoteData>[6] { new(), new(), new(), new(), new(), new() };
    public InstrumentType Instrument { get; set; }
    public DifficultyType Difficulty { get; set; }

    public int TotalSelectionCount => throw new System.NotImplementedException();

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

    public void RemoveTickFromAllSelections(int tick)
    {
        throw new System.NotImplementedException();
    }
}

/*
public class VoxInstrument : IInstrument
{
    // not yet implemented
} */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.UI;
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
    public void ShiftClickSelect(int tick, bool temporary);
    public void ReleaseTemporaryTicks();
    public void RemoveTickFromAllSelections(int tick);

    void SetUpInputMap();
}

public class SyncTrackInstrument : IInstrument
{
    // Lanes located in respective libraries
    // This class is pretty much for shift click and clearing both TS and BPMData selections when needed
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

    public SyncTrackInstrument()
    {
        bpmSelection = new(Tempo.Events);
        tsSelection = new(TimeSignature.Events);

        bpmClipboard = new(Tempo.Events);
        tsClipboard = new(TimeSignature.Events);

       // Tempo.Events.UpdateNeededAtTick += modifiedTick => Tempo.RecalculateTempoEventDictionary(modifiedTick);
    }


    public SelectionSet<BPMData> bpmSelection;
    public SelectionSet<TSData> tsSelection;

    public ClipboardSet<BPMData> bpmClipboard;
    public ClipboardSet<TSData> tsClipboard;

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
        throw new NotImplementedException("Use export functions in Tempo and TimeSignature libraries.");
        // maybe use this instead of individual libraries in future?
    }

    public void ShiftClickSelect(int tick, bool temporary) => ShiftClickSelect(tick);
    public void ReleaseTemporaryTicks() { } // unneeded - no sustains lol

    public void RemoveTickFromAllSelections(int tick) 
    {
        bpmSelection.Remove(tick);
        tsSelection.Remove(tick);
    } // unneeded

    public void SetUpInputMap() { }

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

    InputMap inputMap;

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

        for (int i = 0; i < Lanes.Count; i++)
        {
            var laneIndex = i;
            Lanes.GetLane(i).UpdateNeededAtTick += changedTick => CheckForHopos((LaneOrientation)laneIndex, changedTick);
        }
    }

    public void SetUpInputMap()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.ForceTap.performed += x => ToggleTaps();
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
        var activeLane = Lanes.GetLane((int)lane);

        bool nextTickHopo = false;
        bool currentTickHopo = false;
        bool changedTickExists = Lanes.AnyLaneContainsTick(changedTick);
        bool changedTickChord = changedTickExists ? Lanes.IsTickChord(changedTick) : false; // optimize?

        var ticks = Lanes.GetTickEventBounds(changedTick); // biggest bottleneck here btw

        if (ticks.next != Lanes<FiveFretNoteData>.NO_TICK_EVENT &&
            ticks.next - changedTick < Chart.hopoCutoff) nextTickHopo = true;

        if (ticks.prev != Lanes<FiveFretNoteData>.NO_TICK_EVENT &&
            changedTick - ticks.prev < Chart.hopoCutoff) currentTickHopo = true;

        if (activeLane.Contains(changedTick))
        {
            var parameterLaneTickData = activeLane[changedTick];
            if (!parameterLaneTickData.Default ||
                parameterLaneTickData.Flag == FiveFretNoteData.FlagType.tap)
            {
                currentTickHopo = false;
            }
        }
        
        var flag = currentTickHopo ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;
        SetAllTicksInLaneTo(changedTick, ticks.prev, flag);

        if (IsTickTap(ticks.next)) return;
        var nextFlag = nextTickHopo && changedTickExists ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;

        SetAllTicksInLaneTo(ticks.next, changedTick, nextFlag);
    }

    public bool PreviewTickHopo(LaneOrientation lane, int tick)
    {
        var ticks = Lanes.GetTickEventBounds(tick);

        if (ticks.prev != Lanes<FiveFretNoteData>.NO_TICK_EVENT &&
            tick - ticks.prev < Chart.hopoCutoff &&
            (Lanes.GetTickCountAtTick(tick) == 0 && !Lanes.GetLane((int)lane).Contains(ticks.prev))
            ) return true;

        return false;
    }

    public void ToggleTaps()
    {
        if (Chart.LoadedInstrument != this) return;

        var allTicksSelected = Lanes.GetTotalSelection();

        bool toggleToTaps = true;
        foreach (var tick in allTicksSelected)
        {
            if (IsTickTap(tick)) toggleToTaps = false;
        }

        for (int i = 0; i < Lanes.Count; i++)
        {
            var lane = Lanes.GetLane(i);
            foreach(var tick in allTicksSelected)
            {
                if (!lane.Contains(tick)) continue;

                lane[tick] = toggleToTaps ? lane[tick].ExportWithNewFlag(FiveFretNoteData.FlagType.tap) : lane[tick].ExportWithNewFlag(FiveFretNoteData.FlagType.hopo);
            }
        }

        if (!toggleToTaps) CheckForHoposInRange(allTicksSelected.Min(), allTicksSelected.Max());

        Chart.Refresh();
    }

    public bool IsTickTap(int tick)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            var lane = Lanes.GetLane(i);
            if (!lane.Contains(tick)) continue;

            if (lane[tick].Flag == FiveFretNoteData.FlagType.tap)
            {
                return true;
            }
        }
        return false;
    }

    public void CheckForHoposInRange(int startTick, int endTick)
    {
        var uniqueTicks = Lanes.UniqueTicks;

        int startIndex = uniqueTicks.BinarySearch(startTick);

        if (startIndex < 0)
        {
            startIndex = ~startIndex - 1;
        }

        int endIndex = uniqueTicks.BinarySearch(endTick);
        if (endIndex < 0)
        {
            endIndex = ~endIndex + 1;
        }
        if (endIndex >= uniqueTicks.Count) endIndex = uniqueTicks.Count - 1;

        for (int i = startIndex; i <= endIndex; i++)
        {
            var currentTick = uniqueTicks[i];

            var prevTick = i != 0 ? uniqueTicks[i - 1] : -Chart.hopoCutoff;

            var flag = (currentTick - prevTick < Chart.hopoCutoff) && !Lanes.IsTickChord(currentTick) ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;

            SetAllTicksInLaneTo(currentTick, prevTick, flag);
        }
    }

    void SetAllTicksInLaneTo(int targetTick, int previousTick, FiveFretNoteData.FlagType flag)
    {
        bool isLastTickChord = Lanes.IsTickChord(previousTick);
        bool isCurrentTickChord = Lanes.IsTickChord(targetTick);
        bool settingToTap = flag == FiveFretNoteData.FlagType.tap;

        for (int i = 0; i < Lanes.Count; i++)
        {
            var lane = Lanes.GetLane(i);
            if (!lane.Contains(targetTick)) continue;
            if (!lane[targetTick].Default) break;

            if (((!isLastTickChord && lane.Contains(previousTick)) || isCurrentTickChord) && !settingToTap)
            {
                flag = FiveFretNoteData.FlagType.strum;
            }

            if (lane[targetTick].Flag != flag)
            {
                lane[targetTick] = lane[targetTick].ExportWithNewFlag(flag);
            }
        }
    }

    public void ForceFlag(FiveFretNoteData.FlagType flag)
    {
        if (Chart.LoadedInstrument != this) return;

        var allTicksSelected = Lanes.GetTotalSelection();


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

    public void ToggleTap() { }
    public void ToggleForced() { }
    public void SetUpInputMap() { }
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

    public void ToggleTap() { }
    public void ToggleForced() { }
    public void SetUpInputMap() { }
}

/*
public class VoxInstrument : IInstrument
{
    // not yet implemented
} */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// Each lane has its own set of notes and selections (EventData)
// Lanes are defined with type T.
// Notes are defined/calculated upon on a per-lane basis.

public class FiveFretNote : Event<FiveFretNoteData>, IPoolable
{
    public override MoveData<FiveFretNoteData> GetMoveData() => chartInstrument.InstrumentMoveData[(int)laneIdentifier];
    public override EventData<FiveFretNoteData> GetEventData() => chartInstrument.InstrumentEventData[(int)laneIdentifier];

    public Coroutine destructionCoroutine { get; set; }

    public FiveFretInstrument.LaneOrientation laneIdentifier
    {
        get
        {
            return _li;
        }
        set
        {
            if (noteColorMaterials.Count > 0)
            {
                noteColor.material = noteColorMaterials[(int)value];
                sustainColor.material = noteColorMaterials[(int)value];
            }
            _li = value;
        } 
    }
    FiveFretInstrument.LaneOrientation _li;

    [SerializeField] Transform sustain;
    [SerializeField] MeshRenderer sustainColor;
    [SerializeField] MeshRenderer noteColor;
    [SerializeField] List<Material> noteColorMaterials = new();

    public override IPreviewer EventPreviewer => lanePreviewer;
    public IPreviewer lanePreviewer; // define in pooler

    FiveFretLane parentLane
    {
        get
        {
            if (_lane == null)
            {
                _lane = GetComponentInParent<FiveFretLane>();
            }
            return _lane;
        }
    } // define in pooler
    FiveFretLane _lane;

    public override void RefreshEvents()
    {
        parentLane.UpdateEvents();
    }

    public override SortedDictionary<int, FiveFretNoteData> GetEventSet()
    {
        return chartInstrument.Lanes[(int)laneIdentifier];
    }

    public override void SetEvents(SortedDictionary<int, FiveFretNoteData> newEvents)
    {
        chartInstrument.Lanes[(int)laneIdentifier] = newEvents;
    }

    public void InitializeNote()
    {
        Selected = CheckForSelection();
    }

    public void UpdatePosition(double percentOfTrack, float trackLength, float xPosition)
    {
        var trackProportion = (float)percentOfTrack * trackLength;
        transform.position = new Vector3(xPosition, 0, trackProportion);
    }

    FiveFretInstrument chartInstrument => (FiveFretInstrument)Chart.LoadedInstrument;

    
    public override void OnPointerUp(PointerEventData pointerEventData)
    {
        if (disableNextSelectionCheck)
        {
            disableNextSelectionCheck = false;
            return;
        }

        // undo temporary selection add due to sustain when right-click dragging on a single note (sustaining is based on selection)
        if (pointerEventData.button == PointerEventData.InputButton.Right && chartInstrument.TotalSelectionCount == 1)
        {
            if (GetEventData().Selection.ContainsKey(Tick)) GetEventData().Selection.Remove(Tick);
            RefreshEvents();
            return;
        }

        if (!GetEventData().RMBHeld && pointerEventData.button == PointerEventData.InputButton.Left)
        {
            if (justDeleted)
            {
                justDeleted = false;
                return;
            }
            CalculateSelectionStatus(pointerEventData);
            RefreshEvents();
            Debug.Log($"Just checked {Tick} - {Selected} (Leftclick)");
            return;
        }
    } 

    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);
        if (justDeleted) return;
        
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            if (!GetEventData().Selection.ContainsKey(Tick))
                GetEventData().Selection.Add(Tick, GetEventSet()[Tick]);

            Debug.Log($"Just checked {Tick} - {Selected} (RClick Down)");
        }
    }

    public void UpdateSustain(float trackLength)
    {
        var sustainEndPointTicks = Tick + GetEventSet()[Tick].Sustain;

        var trackProportion = (Tempo.ConvertTickTimeToSeconds(sustainEndPointTicks) - Waveform.startTime) / Waveform.timeShown;
        var trackPosition = trackProportion * trackLength;

        var noteProportion = (Tempo.ConvertTickTimeToSeconds(Tick) - Waveform.startTime) / Waveform.timeShown;
        var notePosition = noteProportion * trackLength;

        var localScaleZ = (float)(trackPosition - notePosition);
        if (localScaleZ + transform.localPosition.z > trackLength) localScaleZ = trackLength - transform.localPosition.z; // stop it from appearing past the end of the highway

        sustain.localScale = new Vector3(sustain.localScale.x, sustain.localScale.y, localScaleZ);
    }

    public override void SustainSelection()
    {
        var sustainData = parentLane.sustainData;

        // Early return if attempting to start a move while over an overlay element
        // Allows moves to start only if interacting with main content
        if (EventPreviewer.IsOverlayUIHit() && !sustainData.sustainInProgress)
        {
            return;
        }

        var newHighwayPercent = EventPreviewer.GetHighwayProportion();

        // 0 is very unlikely as an actual position, but is returned if cursor is outside track (meaning the sustain is temporarily reset - looks weird)
        // could theoretically be possible but even then other checks would fail later in this loop, so just don't do anything 
        if (newHighwayPercent == 0) return; 

        var currentMouseTick = SongTime.CalculateGridSnappedTick(newHighwayPercent);

        // early return if no changes to mouse's grid snap
        if (currentMouseTick == sustainData.lastMouseTick)
        {
            if (sustainData.sustainInProgress && !sustainData.sustainsReset)
            {
                ResetSustains(sustainData); // do it here so that there isn't a flicker before actually sustaining
            }

            sustainData.lastMouseTick = currentMouseTick;
            return;
        }

        if (!sustainData.sustainInProgress)
        {
            sustainData.sustainEventAction = new(GetEventSet());
            sustainData.sustainInProgress = true;
            sustainData.lastMouseTick = currentMouseTick;
            sustainData.firstMouseTick = currentMouseTick;

            sustainData.sustainingTicks.Clear();
            sustainData.sustainingTicks = new(GetEventData().Selection);

            GetEventData().Selection.Clear();
            sustainData.sustainEventAction.CaptureOriginalSustain(sustainData.sustainingTicks.Keys.ToList());

            return;
        }

        var workingEventSet = GetEventSet();
        var ticks = workingEventSet.Keys.ToList();

        if (!sustainData.sustainsReset) // in case the mouse is moved so fast that the check above never runs
        {
            ResetSustains(sustainData);
            return;
        }

        var cursorMoveDifference = currentMouseTick - sustainData.firstMouseTick;

        foreach (var tick in sustainData.sustainingTicks.Keys)
        {
            var newSustain = cursorMoveDifference;

            if (newSustain < -DivisionChanger.CurrentDivision) newSustain = SongTime.SongLengthTicks; // max out sustain, clamping function will take care of the rest

            newSustain = CalculateSustainClamp(newSustain, tick, ticks);

            if (workingEventSet.ContainsKey(tick))
            {
                workingEventSet[tick] = new(newSustain, workingEventSet[tick].Flag, workingEventSet[tick].Default);
            }
        }

        RefreshEvents();
        sustainData.lastMouseTick = currentMouseTick;
    }

    int CalculateSustainClamp(int sustain, int tick, List<int> ticks)
    {
        int nextTickEventIndex = ticks.IndexOf(tick) + 1;

        if (ticks.Count > nextTickEventIndex)
        {
            if (sustain + tick >= ticks[nextTickEventIndex] - UserSettings.SustainGapTicks)
            {
                sustain = (ticks[nextTickEventIndex] - tick) - UserSettings.SustainGapTicks;
            }
        }
        else
        {
            if (sustain + tick >= SongTime.SongLengthTicks)
            {
                sustain = (SongTime.SongLengthTicks - tick); // does sustain gap apply to end of song? 🤔
            }
        }

        return sustain;
    }

    void ResetSustains(SustainData<FiveFretNoteData> sustainData)
    {
        var workingEventSet = GetEventSet();
        foreach (var tick in sustainData.sustainingTicks.Keys)
        {
            if (workingEventSet.ContainsKey(tick))
            {
                workingEventSet[tick] = new(0, workingEventSet[tick].Flag, workingEventSet[tick].Default);
            }
        }
        RefreshEvents();
        sustainData.sustainsReset = true;
    }

    public override void CompleteSustain()
    {
        parentLane.sustainData.sustainInProgress = false;
        parentLane.sustainData.sustainsReset = false;
        GetEventData().Selection = new(parentLane.sustainData.sustainingTicks);
        RefreshEvents();
    }
} 
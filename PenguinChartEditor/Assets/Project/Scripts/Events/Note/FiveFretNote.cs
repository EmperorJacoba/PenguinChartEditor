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
    public override ClipboardSet<FiveFretNoteData> Clipboard => chartInstrument.Lanes.GetLaneClipboard((int)laneIdentifier);
    public override LaneSet<FiveFretNoteData> LaneData => chartInstrument.Lanes.GetLane((int)laneIdentifier);
    public override SelectionSet<FiveFretNoteData> Selection => chartInstrument.Lanes.GetLaneSelection((int)laneIdentifier);

    [SerializeField] float defaultYPos = 0;
    [SerializeField] Transform sustain;
    [SerializeField] MeshRenderer sustainColor;
    [SerializeField] MeshRenderer noteColor;
    [SerializeField] NoteColors colors;
    [SerializeField] GameObject hopoTopper;
    [SerializeField] GameObject strumTopper;
    [SerializeField] GameObject tapTopper;
    [SerializeField] MeshRenderer headBorder;
    [SerializeField] bool previewer;

    public Coroutine destructionCoroutine { get; set; }

    public FiveFretInstrument.LaneOrientation laneIdentifier
    {
        get
        {
            return _li;
        }
        set
        {
            if (_li == value) return;
            if (colors != null)
            {
                noteColor.material = previewer ? colors.GetPreviewerMat(false) : colors.GetNoteMaterial((int)value, IsTap);
                sustainColor.material = previewer ? colors.GetPreviewerMat(false) : colors.GetNoteMaterial((int)value, IsTap);
            }
            _li = value;
        }
    }

    // starts as -1 so the redundancy check in laneIdentifier.set does not return true when setting lane to 0
    FiveFretInstrument.LaneOrientation _li = (FiveFretInstrument.LaneOrientation)(-1);

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
    }
    FiveFretLane _lane;

    public bool IsHopo
    {
        get => _isHopo;
        set
        {
            if (_isHopo == value) return;

            strumTopper.SetActive(!value);
            hopoTopper.SetActive(value);
            _isHopo = value;
        }
    }
    bool _isHopo = false;

    public bool IsTap
    {
        get => _isTap;
        set
        {
            if (_isTap == value) return;

            noteColor.material = previewer ? colors.GetPreviewerMat(value) : colors.GetNoteMaterial((int)laneIdentifier, value);

            // this script is also on opens
            // opens do not have head borders and thus borders will be null
            if (headBorder != null)
            {
                headBorder.material = colors.GetHeadColor(value);
            }

            strumTopper.SetActive(!value);
            tapTopper.SetActive(value);
            _isTap = value;
        }
    }
    bool _isTap = false;

    public override IInstrument ParentInstrument => chartInstrument;

    public override int Tick
    {
        get
        {
            return _tick;
        }
    }
    [SerializeField] int _tick;

    public void InitializeEvent(int tick, float highwayLength, FiveFretInstrument.LaneOrientation lane, IPreviewer previewer)
    {
        _tick = tick;
        Visible = true;
        laneIdentifier = lane;
        lanePreviewer = previewer;

        InitializeNote();

        UpdatePosition(
            Waveform.GetWaveformRatio(tick),
            highwayLength,
            XCoordinate);

        UpdateSustain(highwayLength);

        var tickData = LaneData[tick];
        IsHopo = (tickData.Flag == FiveFretNoteData.FlagType.hopo);
        IsTap = (tickData.Flag == FiveFretNoteData.FlagType.tap);
    }
    public float XCoordinate => Chart.instance.lanePositionReference.GetLaneWorldSpaceXCoordinate((int)laneIdentifier);

    public override void RefreshLane() => parentLane.UpdateEvents();

    public override void SetEvents(SortedDictionary<int, FiveFretNoteData> newEvents)
    {
        chartInstrument.Lanes.SetLane((int)laneIdentifier, newEvents);
    }

    void InitializeNote()
    {
        if (SelectionOverlay != null) Selected = CheckForSelection();
    }

    public void UpdatePosition(double percentOfTrack, float trackLength, float xPosition)
    {
        var trackProportion = (float)percentOfTrack * trackLength;
        transform.position = new Vector3(xPosition, defaultYPos, trackProportion);
    }

    public FiveFretInstrument chartInstrument => (FiveFretInstrument)Chart.LoadedInstrument;

    public override void OnPointerUp(PointerEventData pointerEventData)
    {
        if (disableNextSelectionCheck)
        {
            disableNextSelectionCheck = false;
            return;
        }

        if (justDeleted)
        {
            justDeleted = false;
            return;
        }

        // undo temporary selection add due to sustain when right-click dragging on a single note (sustaining is based on selection)
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            if (chartInstrument.Lanes.TempSustainTicks.Contains(Tick) && Selection.Contains(Tick))
            {
                ParentInstrument.RemoveTickFromAllSelections(Tick);
                ParentInstrument.ReleaseTemporaryTicks();
            }
            chartInstrument.Lanes.TempSustainTicks.Clear();
            RefreshLane();
            return;
        }

        if (pointerEventData.button == PointerEventData.InputButton.Left)
        {
            CalculateSelectionStatus(pointerEventData);
            return;
        }
    } 

    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);
        
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                ParentInstrument.ShiftClickSelect(Tick, true);
                return;
            }
            chartInstrument.Lanes.TempSustainTicks.Add(Tick);
            Selection.Add(Tick, LaneData[Tick]);
        }
    }

    public void UpdateSustain(float trackLength)
    {
        var sustainEndPointTicks = Tick + LaneData[Tick].Sustain;

        var trackProportion = (Tempo.ConvertTickTimeToSeconds(sustainEndPointTicks) - Waveform.startTime) / Waveform.timeShown;
        var trackPosition = trackProportion * trackLength;

        var noteProportion = (Tempo.ConvertTickTimeToSeconds(Tick) - Waveform.startTime) / Waveform.timeShown;
        var notePosition = noteProportion * trackLength;

        var localScaleZ = (float)(trackPosition - notePosition);

        // stop it from appearing past the end of the highway
        if (localScaleZ + transform.localPosition.z > trackLength) localScaleZ = trackLength - transform.localPosition.z; 
        if (localScaleZ < 0) localScaleZ = 0; // box collider negative size issues??

        sustain.localScale = new Vector3(sustain.localScale.x, sustain.localScale.y, localScaleZ);
    }

    // true = start sustain editing from the bottom of the note (sustain = 0)
    // use when editing sustains from the note (root of sustain)
    // false = start sustain editing from mouse cursor/current sustain positioning
    // use when editing sustain from the sustain tail 
    public static bool resetSustains = true;
    public override void SustainSelection()
    {
        if (!Chart.IsEditAllowed()) return;
        var sustainData = parentLane.sustainData;

        // Early return if attempting to start an edit while over an overlay element
        // Allows edit to start only if interacting with main content
        if (EventPreviewer.IsOverlayUIHit() && !sustainData.sustainInProgress)
        {
            return;
        }

        var currentMouseTick = GetCurrentMouseTick();
        if (currentMouseTick == int.MinValue) return;

        // early return if no changes to mouse's grid snap
        if (currentMouseTick == sustainData.lastMouseTick)
        {
            sustainData.lastMouseTick = currentMouseTick;
            return;
        }

        if (!sustainData.sustainInProgress)
        {
            // directly access parent lane here to avoid reassigning the local shortcut variable
            parentLane.sustainData = new(LaneData, Selection, currentMouseTick);
            return;
        }

        var workingEventSet = LaneData;
        var ticks = workingEventSet.Keys.ToList();

        var cursorMoveDifference = currentMouseTick - sustainData.firstMouseTick;

        foreach (var tick in sustainData.sustainingTicks.Keys)
        {
            int sustainOffset = 0;
            if (!resetSustains) sustainOffset = sustainData.firstMouseTick - tick;

            var newSustain = sustainOffset + cursorMoveDifference;

            // drag behind the note to max out sustain - cool feature from moonscraper
            // -CurrentDivison is easy arbitrary value for when to max out - so that there is a buffer for users to remove sustain entirely
            // SongLengthTicks will get clamped to max sustain length
            if (newSustain < -DivisionChanger.CurrentDivision) newSustain = SongTime.SongLengthTicks;

            newSustain = CalculateSustainClamp(newSustain, tick, ticks);

            if (workingEventSet.ContainsKey(tick))
            {
                workingEventSet[tick] = new(newSustain, workingEventSet[tick].Flag, workingEventSet[tick].Default);
            }
        }

        RefreshLane();
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

    public override void CompleteSustain()
    {
        Selection.OverwriteWith(parentLane.sustainData.sustainingTicks);

        foreach (var item in chartInstrument.Lanes.TempSustainTicks)
        {
            Selection.Remove(item);
        }

        // parameterless new() = flag as empty 
        parentLane.sustainData = new();
        RefreshLane();
    }

    public int GetCurrentMouseTick()
    {
        var newHighwayPercent = EventPreviewer.GetCursorHighwayProportion();

        // 0 is very unlikely as an actual position (as 0 is at the very bottom of the TRACK, which should be outside the screen in most cases)
        // but is returned if cursor is outside track
        // min value serves as an easy exit check in case the cursor is outside the highway
        if (newHighwayPercent == 0) return int.MinValue;

        return SongTime.CalculateGridSnappedTick(newHighwayPercent);
    }

    // used on sustain trail itself when click happens on trail
    // click on sustain trail + drag activates SustainSelection() within the previewer object
    public void ClampSustain(int tickLength) =>
        LaneData[Tick] = new(tickLength, LaneData[Tick].Flag, LaneData[Tick].Default);
} 
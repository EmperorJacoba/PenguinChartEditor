using System;
using System.Collections.Generic;
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
        if (!GetEventData().RMBHeld || pointerEventData.button != PointerEventData.InputButton.Left)
        {
            CalculateSelectionStatus(pointerEventData);
            RefreshEvents();
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
        if (localScaleZ + transform.localPosition.z > trackLength) localScaleZ = trackLength - transform.localPosition.z;

        sustain.localScale = new Vector3(sustain.localScale.x, sustain.localScale.y, localScaleZ);
    }

} 
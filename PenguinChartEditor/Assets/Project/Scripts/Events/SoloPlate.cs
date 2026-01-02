using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoloPlate : Event<SoloEventData>
{
    [SerializeField] TMP_Text percentage;
    [SerializeField] TMP_Text counter;

    public void InitializeEvent(SoloSectionLane parentLane, int startTick, int endTick)
    {
        ParentLane = parentLane;

        float zPosition;
        if (SongTime.SongPositionTicks < startTick)
        {
            zPosition = (float)(Waveform.GetWaveformRatio(startTick) * Highway3D.highwayLength);
        }
        else if (SongTime.SongPositionTicks > endTick)
        {
            zPosition = (float)(Waveform.GetWaveformRatio(endTick) * Highway3D.highwayLength);
        }
        else
        {
            zPosition = Mathf.Floor((float)Waveform.GetWaveformRatio(SongTime.SongPositionTicks) * Highway3D.highwayLength);
        }
        _tick = startTick;

        List<int> ticks = ParentInstrument.UniqueTicks;
        var totalNotes = ticks.Where(x => x >= startTick && x <= endTick).Count();
        var notesHit = ticks.Where(x => x >= startTick && x <= SongTime.SongPositionTicks).Count();

        transform.position = new(transform.position.x, transform.position.y, zPosition);
        percentage.text = $"{Mathf.Floor((notesHit / (float)totalNotes) * 100)}%";
        counter.text = $"{notesHit} / {totalNotes}";

        CheckForSelection();
    }

    public override int Lane => 0;

    public SoloSectionLane ParentLane { get; set; }
    public GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;
    public override IInstrument ParentInstrument => parentGameInstrument.representedInstrument;


    public override SelectionSet<SoloEventData> Selection => ParentInstrument.SoloData.SelectedStartEvents;

    public override LaneSet<SoloEventData> LaneData => ParentInstrument.SoloData.SoloEvents;

    public SoloPreviewer previewer
    {
        get => _prevobj;
        set
        {
            if (_prevobj == value) return;
            _prevobj = value;
        }
    } // define in pooler
    SoloPreviewer _prevobj;


    public override void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(1) && eventData.button == PointerEventData.InputButton.Left)
        {
            var targetEvent = LaneData.Where(x => x.Value.StartTick == Tick).ToList();
            if (targetEvent.Count == 0) return;

            LaneData.Remove(targetEvent[0]);
            return;
        }

        CalculateSelectionStatus(eventData);
    }

    public override void RefreshLane() => ParentLane.UpdateEvents();

    public override void CreateEvent(int newTick, SoloEventData newData) { } // please remove
}

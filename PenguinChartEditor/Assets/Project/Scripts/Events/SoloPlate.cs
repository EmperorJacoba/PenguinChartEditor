using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoloPlate : Event<SoloEventData>
{
    [SerializeField] TMP_Text percentage;
    [SerializeField] TMP_Text counter;

    public void InitializeEvent(int startTick, int endTick)
    {
        float zPosition;
        if (SongTime.SongPositionTicks < startTick)
        {
            zPosition = (float)(Waveform.GetWaveformRatio(startTick) * Chart.instance.SceneDetails.HighwayLength);
        }
        else if (SongTime.SongPositionTicks > endTick)
        {
            zPosition = (float)(Waveform.GetWaveformRatio(endTick) * Chart.instance.SceneDetails.HighwayLength);
        }
        else
        {
            zPosition = Mathf.Floor((float)Waveform.GetWaveformRatio(SongTime.SongPositionTicks) * Chart.instance.SceneDetails.HighwayLength);
        }
        _tick = startTick;

        List<int> ticks = Chart.LoadedInstrument.UniqueTicks;
        var totalNotes = ticks.Where(x => x >= startTick && x <= endTick).Count();
        var notesHit = ticks.Where(x => x >= startTick && x <= SongTime.SongPositionTicks).Count();

        transform.position = new(transform.position.x, transform.position.y, zPosition);
        percentage.text = $"{Mathf.Floor((notesHit / (float)totalNotes) * 100)}%";
        counter.text = $"{notesHit} / {totalNotes}";

        Selected = CheckForSelection();
    }

    public override int Lane => 0;

    public override SelectionSet<SoloEventData> Selection => Chart.LoadedInstrument.SoloData.SelectedStartEvents;

    public override LaneSet<SoloEventData> LaneData => Chart.LoadedInstrument.SoloData.SoloEvents;

    public override IPreviewer EventPreviewer => (IPreviewer)previewer;
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

    public override IInstrument ParentInstrument => Chart.LoadedInstrument;

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(1) && eventData.button == PointerEventData.InputButton.Left)
        {
            var targetEvent = Chart.LoadedInstrument.SoloData.SoloEvents.Where(x => x.Value.StartTick == Tick).ToList();
            if (targetEvent.Count == 0) return;

            Chart.LoadedInstrument.SoloData.SoloEvents.Remove(targetEvent[0]);
            return;
        }

        CalculateSelectionStatus(eventData);
    }

    public override void RefreshLane() => SoloSectionSpawner.instance.UpdateEvents();

    public override void CreateEvent(int newTick, SoloEventData newData) { } // please remove
}

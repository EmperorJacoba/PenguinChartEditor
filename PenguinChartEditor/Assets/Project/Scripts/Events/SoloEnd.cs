using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoloEnd : Event<SoloEventData>
{
    public override int Lane => 0;

    public override SelectionSet<SoloEventData> Selection => Chart.LoadedInstrument.SoloData.SelectedEndEvents;

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

    public override void CreateEvent(int newTick, SoloEventData newData) { } // unused - please remove from top-level

    public int representedTick;
    public void InitializeEvent(int startTick, int endTick)
    {
        double ratio = Waveform.GetWaveformRatio(endTick);
        if (ratio > 1)
        {
            Visible = false;
            return;
        }

        // Selection uses the startTick as the ID for all solo events.
        // The tick this represents is the end tick, but the selection depends on the start tick for continuity.
        _tick = startTick;
        representedTick = endTick;

        float zPosition = (float)(Waveform.GetWaveformRatio(endTick) * Chart.instance.SceneDetails.HighwayLength);
        transform.position = new(transform.position.x, transform.position.y, zPosition);

        Selected = CheckForSelection();
        Visible = true;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(1) && eventData.button == PointerEventData.InputButton.Left)
        {
            var targetEvent = Chart.LoadedInstrument.SoloData.SoloEvents.Where(x => x.Value.EndTick == representedTick).ToList();
            if (targetEvent.Count == 0) return;

            var endTick = SongTime.SongLengthTicks - targetEvent[0].Value.StartTick;
            var nextSoloEvent = Chart.LoadedInstrument.SoloData.SoloEvents.Where(x => x.Value.StartTick > representedTick);

            if (nextSoloEvent.Count() > 0) endTick = nextSoloEvent.Min(x => x.Value.StartTick) - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

            var replacingEvent = new SoloEventData(targetEvent[0].Value.StartTick, endTick);

            Chart.LoadedInstrument.SoloData.SoloEvents.Remove(targetEvent[0]);
            Chart.LoadedInstrument.SoloData.SoloEvents.Add(replacingEvent.StartTick, replacingEvent);

            return;
        }

        CalculateSelectionStatus(eventData);
    }

    public override void RefreshLane() => SoloSectionSpawner.instance.UpdateEvents();
}
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoloEnd : Event<SoloEventData>
{
    public override int Lane => 0;

    public override SelectionSet<SoloEventData> Selection => Chart.LoadedInstrument.SoloEventSelection;

    public override LaneSet<SoloEventData> LaneData => Chart.LoadedInstrument.SoloEvents;

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

    public override IInstrument ParentInstrument => throw new System.NotImplementedException();

    public override void CreateEvent(int newTick, SoloEventData newData) { } // unused - please remove from top-level

    public void InitializeEvent(int startTick, int endTick)
    {
        double ratio = Waveform.GetWaveformRatio(endTick);
        if (ratio > 1)
        {
            Visible = false;
            return;
        }

        _tick = startTick;

        float zPosition = (float)(Waveform.GetWaveformRatio(endTick) * Chart.instance.SceneDetails.HighwayLength);
        transform.position = new(transform.position.x, transform.position.y, zPosition);

        Visible = true;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(1) && eventData.button == PointerEventData.InputButton.Left)
        {
            var targetEvent = Chart.LoadedInstrument.SoloEvents.Where(x => x.Value.EndTick == Tick).ToList();
            if (targetEvent.Count == 0) return;

            var endTick = SongTime.SongLengthTicks;
            var nextSoloEvent = Chart.LoadedInstrument.SoloEvents.Where(x => x.Value.StartTick > Tick);

            if (nextSoloEvent.Count() > 0) endTick = nextSoloEvent.Min(x => x.Value.StartTick) - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

            var replacingEvent = new SoloEventData(targetEvent[0].Value.StartTick, endTick);

            Chart.LoadedInstrument.SoloEvents.Remove(targetEvent[0]);
            Chart.LoadedInstrument.SoloEvents.Add(replacingEvent.StartTick, replacingEvent);

            return;
        }

        CalculateSelectionStatus(eventData);
    }

    public override void RefreshLane() => SoloSectionSpawner.instance.UpdateEvents();
}
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoloEnd : Event<SoloEventData>
{
    public override bool hasSustainTrail => false;
    public override int Lane => IInstrument.SOLO_DATA_LANE_ID;
    public GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;

    public override SelectionSet<SoloEventData> Selection => ParentInstrument.SoloData.SelectedEndEvents;

    public override LaneSet<SoloEventData> LaneData => ParentInstrument.SoloData.SoloEvents;

    public override IInstrument ParentInstrument => parentGameInstrument.representedInstrument;

    public override void CreateEvent(int newTick, SoloEventData newData) { } // unused - please remove from top-level

    public int representedTick;
    public void InitializeEvent(SoloSectionLane parentLane, int startTick, int endTick)
    {
        ParentLane = parentLane;

        // Selection uses the startTick as the ID for all solo events.
        // The tick this represents is the end tick, but the selection depends on the start tick for continuity.
        _tick = startTick;
        representedTick = endTick;

        UpdatePosition();

        CheckForSelection();
    }

    public void UpdatePosition()
    {
        double ratio = Waveform.GetWaveformRatio(representedTick);
        if (ratio >= 1)
        {
            Visible = false;
            return;
        }
        Visible = true;

        float zPosition = (float)(ratio * Highway3D.highwayLength);
        transform.position = new(transform.position.x, transform.position.y, zPosition);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(1) && eventData.button == PointerEventData.InputButton.Left)
        {
            var targetEvent = LaneData.Where(x => x.Value.EndTick == representedTick).ToList();
            if (targetEvent.Count == 0) return;

            var endTick = SongTime.SongLengthTicks - targetEvent[0].Value.StartTick;
            var nextSoloEvent = LaneData.Where(x => x.Value.StartTick > representedTick);

            if (nextSoloEvent.Count() > 0) endTick = nextSoloEvent.Min(x => x.Value.StartTick) - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

            var replacingEvent = new SoloEventData(targetEvent[0].Value.StartTick, endTick);

            LaneData.Remove(targetEvent[0]);
            LaneData.Add(replacingEvent.StartTick, replacingEvent);

            return;
        }

        CalculateSelectionStatus(eventData);
    }
}
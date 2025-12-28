using System.Linq;
using UnityEngine;

public class SoloPreviewer : Previewer
{
    [SerializeField] SoloPlate previewSoloPlate;
    [SerializeField] SoloEnd previewEndPlate;

    protected override void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        previewEndPlate.IsPreviewEvent = true;
        previewSoloPlate.IsPreviewEvent = true;
        // event reference removed - there are multiple references

        inputMap.Charting.PreviewMousePos.performed += position =>
            UpdatePosition();

        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();
    }

    protected override bool IsPreviewerVisible()
    {
        return previewEndPlate.Visible || previewSoloPlate.Visible;
    }

    protected override void RemoveTickFromSelection()
    {
        previewSoloPlate.Selection.Remove(Tick);
    }

    protected override void UpdatePreviewer()
    {
        if (Chart.instance.SceneDetails.GetCursorHighwayPosition().x < Chart.instance.SceneDetails.highwayRightEndCoordinate)
        {
            Hide();
            return;
        }

        var highwayProportion = Chart.instance.SceneDetails.GetCursorHighwayProportion();

        if (highwayProportion == 0)
        {
            Hide();
            return;
        }

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);
        var percentOfTrack = Waveform.GetWaveformRatio(Tick);
        var zPosition = (float)percentOfTrack * Chart.instance.SceneDetails.HighwayLength;

        var activeSoloEvents = Chart.LoadedInstrument.SoloEvents.Where(x => x.Value.StartTick <= Tick && x.Value.EndTick >= Tick);

        if (activeSoloEvents.Count() == 0)
        {
            previewSoloPlate.transform.position = new(previewSoloPlate.transform.position.x, previewSoloPlate.transform.position.y, zPosition);
            previewSoloPlate.Visible = true;
            previewEndPlate.Visible = false;
        }
        else
        {
            previewEndPlate.transform.position = new(previewEndPlate.transform.position.x, previewEndPlate.transform.position.y, zPosition);
            previewEndPlate.Visible = true;
            previewSoloPlate.Visible = false;
        }
    }

    protected override void AddCurrentEventDataToLaneSet()
    {
        var activeSoloEvents = Chart.LoadedInstrument.SoloEvents.Where(x => x.Value.StartTick <= Tick && x.Value.EndTick >= Tick);

        if (activeSoloEvents.Count() == 0)
        {
            var endTick = SongTime.SongLengthTicks;
            var nextSoloEvent = Chart.LoadedInstrument.SoloEvents.Where(x => x.Value.StartTick > Tick);

            if (nextSoloEvent.Count() > 0) endTick = nextSoloEvent.Min(x => x.Value.StartTick) - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

            Chart.LoadedInstrument.SoloEvents.Add(Tick, new(Tick, endTick));
        }
        else
        {
            var soloEventList = activeSoloEvents.Select(x => x.Key).ToList();

            var currentEvent = Chart.LoadedInstrument.SoloEvents[soloEventList[0]];
            if (currentEvent.StartTick == Tick) return;

            var replacingEvent = new SoloEventData(currentEvent.StartTick, Tick);

            Chart.LoadedInstrument.SoloEvents.Remove(soloEventList[0]);
            Chart.LoadedInstrument.SoloEvents.Add(replacingEvent.StartTick, replacingEvent);
        }
    }

    public override void Hide()
    {
        previewEndPlate.Visible = false;
        previewSoloPlate.Visible = false;
    }

    public override void Show() => throw new System.NotSupportedException(
        "Show() cannot be called on SoloPreviewer. SoloPreviewer is made up of multiple events shown depending on its position on the track. Use individual visible attributes."
        );
}
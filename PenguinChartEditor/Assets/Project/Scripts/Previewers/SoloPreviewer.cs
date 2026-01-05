using System.Linq;
using UnityEngine;

public class SoloPreviewer : Previewer
{
    [SerializeField] SoloPlate previewSoloPlate;
    [SerializeField] SoloEnd previewEndPlate;
    SoloSectionLane ParentLane { get; set; }
    IInstrument ParentInstrument => ParentLane.parentGameInstrument.representedInstrument;
    GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;

    protected override void Awake()
    {
        ParentLane = GetComponentInParent<SoloSectionLane>();

        inputMap = new();
        inputMap.Enable();

        previewSoloPlate.ParentLane = ParentLane;
        previewEndPlate.IsPreviewEvent = true;
        previewSoloPlate.IsPreviewEvent = true;

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
        // Gets called before the selection is actually assigned sometimes.
        previewSoloPlate.Selection.Remove(Tick);
    }

    protected override void UpdatePreviewer()
    {
        if (Chart.instance.SceneDetails.GetCursorHighwayPosition().x < parentGameInstrument.HighwayRightEndCoordinate || !UserSettings.SoloPlacingAllowed)
        {
            Hide();
            return;
        }

        var highwayProportion = parentGameInstrument.GetCursorHighwayProportion();

        if (highwayProportion == 0)
        {
            Hide();
            return;
        }

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);
        var percentOfTrack = Waveform.GetWaveformRatio(Tick);
        var zPosition = (float)percentOfTrack * Highway3D.highwayLength;

        var activeSoloEvents = ParentInstrument.SoloData.SoloEvents.Where(x => x.Value.StartTick <= Tick && x.Value.EndTick >= Tick);

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
        var activeSoloEvents = ParentInstrument.SoloData.SoloEvents.Where(x => x.Value.StartTick <= Tick && x.Value.EndTick >= Tick);

        if (activeSoloEvents.Count() == 0)
        {
            var endTick = SongTime.SongLengthTicks;
            var nextSoloEvent = ParentInstrument.SoloData.SoloEvents.Where(x => x.Value.StartTick > Tick);

            if (nextSoloEvent.Count() > 0) endTick = nextSoloEvent.Min(x => x.Value.StartTick) - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

            ParentInstrument.SoloData.SoloEvents.Add(Tick, new(Tick, endTick));
        }
        else
        {
            var soloEventList = activeSoloEvents.Select(x => x.Key).ToList();

            var currentEvent = ParentInstrument.SoloData.SoloEvents[soloEventList[0]];
            if (currentEvent.StartTick == Tick) return;

            var replacingEvent = new SoloEventData(currentEvent.StartTick, Tick);

            ParentInstrument.SoloData.SoloEvents.Remove(soloEventList[0]);
            ParentInstrument.SoloData.SoloEvents.Add(replacingEvent.StartTick, replacingEvent);
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
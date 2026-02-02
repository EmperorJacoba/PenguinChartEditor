using System.Linq;
using UnityEngine;

public class SoloPreviewer : Previewer
{
    [SerializeField] private SoloPlate previewSoloPlate;
    [SerializeField] private SoloEnd previewEndPlate;
    private SoloSectionLane ParentLane { get; set; }
    private IInstrument ParentInstrument => ParentLane.parentGameInstrument.representedInstrument;

    protected override void Awake()
    {
        ParentLane = GetComponentInParent<SoloSectionLane>();

        inputMap = new InputMap();
        inputMap.Enable();

        previewSoloPlate.ParentLane = ParentLane;
        previewEndPlate.IsPreviewEvent = true;
        previewSoloPlate.IsPreviewEvent = true;

        previewerEventReference = previewSoloPlate;

        inputMap.Charting.PreviewMousePos.performed += position =>
            UpdatePosition();

        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();
    }

    protected override bool IsPreviewerVisible()
    {
        return previewEndPlate.Visible || previewSoloPlate.Visible;
    }

    protected override void UpdatePreviewer()
    {
        var activeSoloEvents = 
            ParentInstrument.SoloData.SoloEvents.Where
            (
                x => x.Value.StartTick <= previewTick && x.Value.EndTick >= previewTick
            );

        var platePos = new Vector3(previewSoloPlate.transform.position.x, previewSoloPlate.transform.position.y, (float)Waveform.GetWaveformRatio(previewTick) * Highway3D.highwayLength);

        var isSoloEventActive = activeSoloEvents.Any();

        previewSoloPlate.Visible = !isSoloEventActive;
        previewEndPlate.Visible = isSoloEventActive;
     
        previewSoloPlate.transform.position = platePos;
        previewEndPlate.transform.position = platePos;
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        return !(parentGameInstrument.GetCursorHighwayPosition().x < parentGameInstrument.HighwayRightEndCoordinate) &&
               UserSettings.SoloPlacingAllowed;
    }

    protected override void AddCurrentEventDataToLaneSet()
    {
        var activeSoloEvents = ParentInstrument.SoloData.SoloEvents.Where(x => x.Value.StartTick <= previewTick && x.Value.EndTick >= previewTick);

        if (activeSoloEvents.Count() == 0)
        {
            var endTick = SongTime.SongLengthTicks;
            var nextSoloEvent = ParentInstrument.SoloData.SoloEvents.Where(x => x.Value.StartTick > previewTick);

            if (nextSoloEvent.Count() > 0) endTick = nextSoloEvent.Min(x => x.Value.StartTick) - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

            ParentInstrument.SoloData.SoloEvents.Add(previewTick, new SoloEventData(previewTick, endTick));
        }
        else
        {
            var soloEventList = activeSoloEvents.Select(x => x.Key).ToList();

            var currentEvent = ParentInstrument.SoloData.SoloEvents[soloEventList[0]];
            if (currentEvent.StartTick == previewTick) return;

            var replacingEvent = new SoloEventData(currentEvent.StartTick, previewTick);

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
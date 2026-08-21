using System.Collections.Generic;
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
        
        previewSoloPlate.ParentLane = ParentLane;
        previewEndPlate.IsPreviewEvent = true;
        previewSoloPlate.IsPreviewEvent = true;

        previewerEventReference = previewSoloPlate;
    }

    protected override bool IsPreviewerVisible()
    {
        return previewEndPlate.Visible || previewSoloPlate.Visible;
    }

    protected override void UpdatePreviewer()
    {
        var platePos = new Vector3(
            previewSoloPlate.transform.position.x, 
            previewSoloPlate.transform.position.y, 
            (float)Waveform.GetWaveformRatio(previewTick) * Highway.highwayLength
            );
     
        previewSoloPlate.transform.position = platePos;
        previewEndPlate.transform.position = platePos;
    }

    public override void Show()
    {
        var isPreviewerInOpenSoloEvent = 
            ParentInstrument.SoloData.SoloEvents.Any<KeyValuePair<int, SoloEventData>>(x => x.Value.StartTick <= previewTick && x.Value.EndTick >= previewTick);
        
        previewSoloPlate.Visible = !isPreviewerInOpenSoloEvent;
        previewEndPlate.Visible = isPreviewerInOpenSoloEvent;
    }

    // FIXME: Fix this so that UpdatePreviewer() & CreateEvent() doesn't have to be overriden - maybe change the preview event to
    // a proper SoloEvent and then have it handle how to show a preview event there, depending on the data it's given?
    protected override IEventData GetPreviewData()
    {
        throw new System.NotImplementedException("SoloPreviewer uses its own methods to update itself.");
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        return !(parentGameInstrument.GetCursorHighwayPosition().x < parentGameInstrument.HighwayRightEndCoordinate) &&
               Chart.settings.SoloPlacingAllowed;
    }
    

    public override void CreateEvent()
    {
        if (Chart.IsSceneOverlayUIHit() || !Chart.IsPlacementAllowed()) return;
        if (!IsPreviewerVisible()) return;
        
        // I have set up solo events weirdly so that they can be bundled together as one data. Please fix this
        // so that solo data itself handles all this crap. Then this will no longer need to be overriden.
        // Technically works so that's why I'm not fixing it now. 
        
        var activeSoloEvents = ParentInstrument.SoloData.SoloEvents.Where<KeyValuePair<int, SoloEventData>>(x => x.Value.StartTick <= previewTick && x.Value.EndTick >= previewTick);

        if (!activeSoloEvents.Any())
        {
            var endTick = SongTime.SongLengthTicks - previewTick;
            var nextSoloEvent = ParentInstrument.SoloData.SoloEvents.Where<KeyValuePair<int, SoloEventData>>(x => x.Value.StartTick > previewTick);

            if (nextSoloEvent.Any()) endTick = nextSoloEvent.Min(x => x.Value.StartTick) - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

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
        
        previewerEventReference.RemoveFromSelection();
        Chart.InPlaceRefresh();
    }

    public override void Hide()
    {
        previewEndPlate.Visible = false;
        previewSoloPlate.Visible = false;
    }
}
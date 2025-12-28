using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Overlays;
using UnityEngine;

public class SoloPreviewer : MonoBehaviour
{
    InputMap inputMap;
    [SerializeField] GameObject previewSoloPlate;
    [SerializeField] GameObject previewEndPlate;

    public int Tick { get; set; }

    public void AddCurrentEventDataToLaneSet()
    {

    }

    void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.PreviewMousePos.performed += position =>
            UpdatePosition(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);

        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();
    }

    void UpdatePosition(float mouseRatioY, float mouseRatioX)
    {
        if (!Previewer.IsPreviewerActive())
        {
            HideAll();
            return;
        }

        if (Chart.instance.SceneDetails.GetCursorHighwayPosition().x < Chart.instance.SceneDetails.highwayRightEndCoordinate)
        {
            HideAll();
            return;
        }

        var highwayProportion = Chart.instance.SceneDetails.GetCursorHighwayProportion();

        if (highwayProportion == 0)
        {
            HideAll();
            return;
        }

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);
        var percentOfTrack = Waveform.GetWaveformRatio(Tick);
        var zPosition = (float)percentOfTrack * Chart.instance.SceneDetails.HighwayLength;

        var activeSoloEvents = Chart.LoadedInstrument.SoloEvents.Where(x => x.StartTick <= Tick && x.EndTick >= Tick);

        if (activeSoloEvents.Count() == 0)
        {
            previewSoloPlate.transform.position = new(previewSoloPlate.transform.position.x, previewSoloPlate.transform.position.y, zPosition);
            ShowPlate();
            HideEnd();
        }
        else
        {
            previewEndPlate.transform.position = new(previewEndPlate.transform.position.x, previewEndPlate.transform.position.y, zPosition);
            ShowEnd();
            HidePlate();
        }
    }

    protected void CreateEvent()
    {
        var activeSoloEvents = Chart.LoadedInstrument.SoloEvents.Where(x => x.StartTick <= Tick && x.EndTick >= Tick);

        if (activeSoloEvents.Count() == 0)
        {
            var endTick = SongTime.SongLengthTicks;
            var nextSoloEvent = Chart.LoadedInstrument.SoloEvents.Where(x => x.StartTick > Tick);

            if (nextSoloEvent.Count() > 0) endTick = nextSoloEvent.ToList()[0].StartTick - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

            Chart.LoadedInstrument.SoloEvents.Add(new(Tick, endTick));
        }
        else
        {
            var soloEventList = activeSoloEvents.ToList();
            var replacingEvent = new SoloEvent(soloEventList[0].StartTick, Tick);

            Chart.LoadedInstrument.SoloEvents.Remove(soloEventList[0]);
            Chart.LoadedInstrument.SoloEvents.Add(replacingEvent);
        }

        Chart.Refresh();
    }

    void HideAll()
    {
        HideEnd();
        HidePlate();
    }

    void HidePlate()
    {
        if (previewSoloPlate.activeInHierarchy) previewSoloPlate.SetActive(false);
    }

    void ShowPlate()
    {
        if (!previewSoloPlate.activeInHierarchy) previewSoloPlate.SetActive(true);
    }

    void HideEnd()
    {
        if (previewEndPlate.activeInHierarchy) previewEndPlate.SetActive(false);
    }

    void ShowEnd()
    {
        if (!previewEndPlate.activeInHierarchy) previewEndPlate.SetActive(true);
    }
}
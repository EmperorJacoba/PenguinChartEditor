using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoloSection : MonoBehaviour, IPoolable
{
    private const float HEIGHT_VISIBILITY_OFFSET = 0f;
    [SerializeField] GameObject overlay;
    [SerializeField] SoloPlate platehead;
    [SerializeField] GameObject plateheadReceiver;

    public bool Visible
    {
        get => gameObject.activeInHierarchy;
        set
        {
            if (Visible == value) return;
            gameObject.SetActive(value);
        }
    }

    public int Tick { get; set; }
    public Coroutine destructionCoroutine { get; set; }

    public void UpdateProperties(int startTick, int endTick)
    {
        UpdateOverlayProperties(startTick, endTick);
        UpdatePlateheadPosition(startTick, endTick);
        UpdateReceiverPosition(endTick);
    }

    private void UpdateOverlayProperties(int startTick, int endTick)
    {
        float startPosition = startTick > Waveform.startTick ? (float)(Waveform.GetWaveformRatio(startTick) * Chart.instance.SceneDetails.HighwayLength) : 0;
        overlay.transform.position = new(Chart.instance.SceneDetails.highway.position.x, HEIGHT_VISIBILITY_OFFSET, startPosition);

        var trackProportion = Waveform.GetWaveformRatio(endTick);
        var trackEndPosition = trackProportion * Chart.instance.SceneDetails.HighwayLength;

        var localScaleZ = (float)(trackEndPosition - startPosition);

        // stop it from appearing past the end of the highway
        if (localScaleZ + overlay.transform.position.z > Chart.instance.SceneDetails.HighwayLength)
            localScaleZ = Chart.instance.SceneDetails.HighwayLength - overlay.transform.position.z;

        if (localScaleZ < 0) localScaleZ = 0; // box collider negative size issues??

        overlay.transform.localScale = new(Chart.instance.SceneDetails.highway.localScale.x, 1f, localScaleZ);
    }

    private void UpdatePlateheadPosition(int startTick, int endTick)
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

        List<int> ticks = Chart.LoadedInstrument.UniqueTicks;
        var tickCount = ticks.Where(x => x >= startTick && x <= endTick).Count();
        var ticksHit = ticks.Where(x => x >= startTick && x <= SongTime.SongPositionTicks).Count();

        platehead.UpdatePositionAndText(zPosition, notesHit: ticksHit, totalNotes: tickCount);
    }

    private void UpdateReceiverPosition(int endTick)
    {
        double ratio = Waveform.GetWaveformRatio(endTick);
        if (ratio > 1) 
        {
            if (plateheadReceiver.activeInHierarchy) plateheadReceiver.SetActive(false);
            return;
        }

        if (!plateheadReceiver.activeInHierarchy) plateheadReceiver.SetActive(true);

        float zPosition = (float)(Waveform.GetWaveformRatio(endTick) * Chart.instance.SceneDetails.HighwayLength);
        plateheadReceiver.transform.position = new(plateheadReceiver.transform.position.x, platehead.transform.position.y, zPosition);
    }
}
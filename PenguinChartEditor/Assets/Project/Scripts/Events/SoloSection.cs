using UnityEngine;

public class SoloSection : MonoBehaviour, IPoolable
{
    private const float HEIGHT_VISIBILITY_OFFSET = 0f;
    [SerializeField] GameObject overlay;
    [SerializeField] SoloPlate platehead;
    [SerializeField] SoloEnd plateheadReceiver;

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

    public void UpdateProperties(SoloSectionLane parentLane, int startTick, int endTick)
    {
        UpdateOverlayProperties(startTick, endTick);
        platehead.InitializeEvent(parentLane, startTick, endTick);
        plateheadReceiver.InitializeEvent(parentLane, startTick, endTick);
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

}
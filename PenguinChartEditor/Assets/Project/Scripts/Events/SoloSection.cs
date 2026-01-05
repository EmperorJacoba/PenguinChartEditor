using UnityEngine;

public class SoloSection : MonoBehaviour, IPoolable
{
    private const float HEIGHT_VISIBILITY_OFFSET = 0f;
    [SerializeField] GameObject overlay;
    [SerializeField] SoloPlate platehead;
    [SerializeField] SoloEnd plateheadReceiver;
    SoloSectionLane parentLane;

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

    public void InitializeProperties(ILane parentLane)
    {
        this.parentLane = (SoloSectionLane)parentLane;
    }

    public void InitializeEvent(int tick)
    {
        var soloData = parentLane.parentInstrument.SoloData.SoloEvents[tick];
        int startTick = soloData.StartTick;
        int endTick = soloData.EndTick;

        UpdateOverlayProperties(startTick, endTick);
        platehead.InitializeEvent(parentLane, startTick, endTick);
        plateheadReceiver.InitializeEvent(parentLane, startTick, endTick);
    }

    private void UpdateOverlayProperties(int startTick, int endTick)
    {
        float startPosition = startTick > Waveform.startTick ? (float)(Waveform.GetWaveformRatio(startTick) * Highway3D.highwayLength) : 0;
        overlay.transform.position = new(parentLane.parentGameInstrument.HighwayGlobalTransformProperties.x, HEIGHT_VISIBILITY_OFFSET, startPosition);

        var trackProportion = Waveform.GetWaveformRatio(endTick);
        var trackEndPosition = trackProportion * Highway3D.highwayLength;

        var localScaleZ = (float)(trackEndPosition - startPosition);

        // stop it from appearing past the end of the highway
        if (localScaleZ + overlay.transform.position.z > Highway3D.highwayLength)
            localScaleZ = Highway3D.highwayLength - overlay.transform.position.z;

        if (localScaleZ < 0) localScaleZ = 0; // box collider negative size issues??

        overlay.transform.localScale = new(parentLane.parentGameInstrument.HighwayLocalScaleProperties.x, 1f, localScaleZ);
    }

    void IPoolable.UpdatePosition()
    {
        platehead.UpdatePosition(plateheadReceiver.representedTick);
        plateheadReceiver.UpdatePosition();
    }
}
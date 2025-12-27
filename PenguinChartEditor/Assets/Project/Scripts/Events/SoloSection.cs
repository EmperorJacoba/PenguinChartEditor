using UnityEngine;

public class SoloSection : MonoBehaviour, IPoolable
{
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
        float startPosition = startTick > Waveform.startTick ? (float)(Waveform.GetWaveformRatio(startTick) * Chart.instance.SceneDetails.HighwayLength) : 0;
        transform.position = new(transform.position.x, transform.position.y, startPosition);

        var trackProportion = Waveform.GetWaveformRatio(endTick);
        var trackEndPosition = trackProportion * Chart.instance.SceneDetails.HighwayLength;

        var localScaleZ = (float)(trackEndPosition - startPosition);

        // stop it from appearing past the end of the highway
        if (localScaleZ + transform.localPosition.z > Chart.instance.SceneDetails.HighwayLength)
            localScaleZ = Chart.instance.SceneDetails.HighwayLength - transform.localPosition.z;

        if (localScaleZ < 0) localScaleZ = 0; // box collider negative size issues??

        transform.localScale = new(Chart.instance.SceneDetails.highway.localScale.x, 1f, localScaleZ);
    }
}
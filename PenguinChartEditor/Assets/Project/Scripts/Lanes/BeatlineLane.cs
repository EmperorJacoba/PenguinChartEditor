using UnityEngine;

public class BeatlineLane : BaseBeatlineLane<Beatline> // BPMData is not acted upon here - any calls to it happen in base.Awake()
{
    [SerializeField] BeatlinePooler pooler;

    protected override IPooler<Beatline> Pooler => pooler;

    protected override void InitializeEvent(Beatline @event, int tick) => @event.InitializeEvent(tick, Chart.instance.SceneDetails.HighwayLength, null);
}

using UnityEngine;

public class BeatlineLane : BaseBeatlineLane<Beatline> // BPMData is not acted upon here - any calls to it happen in base.Awake()
{
    public static BeatlineLane instance;
    [SerializeField] BeatlinePooler pooler;

    protected override IPooler<Beatline> Pooler => (IPooler<Beatline>)pooler;

    protected override void Awake()
    {
        // do not call base - no concrete event type to process requests for
        instance = this;
    }

    protected override void InitializeEvent(Beatline @event, int tick) => @event.InitializeEvent(tick);
}

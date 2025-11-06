using UnityEngine;

public class BeatlineLane3D : BaseBeatlineLane<Beatline3D>
{
    public static BeatlineLane3D instance;

    [SerializeField] BeatlinePooler3D pooler;

    protected override IPooler<Beatline3D> Pooler => (IPooler<Beatline3D>)pooler;

    protected override void Awake()
    {
        // do not call base - no concrete event type to process requests for
        instance = this;
    }

    protected override void InitializeEvent(Beatline3D @event, int tick) => @event.InitializeEvent(tick, HighwayLength);
}

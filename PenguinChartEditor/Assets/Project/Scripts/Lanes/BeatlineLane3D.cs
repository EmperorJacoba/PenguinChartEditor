using UnityEngine;

public class BeatlineLane3D : BaseBeatlineLane<Beatline3D>
{
    [SerializeField] BeatlinePooler3D pooler;

    protected override IPooler<Beatline3D> Pooler => pooler;

    protected override void InitializeEvent(Beatline3D @event, int tick) => @event.InitializeEvent(tick, parentGameInstrument.HighwayLength);
}

using UnityEngine;

public class BeatlineLane : BaseBeatlineLane<Beatline> 
{
    [SerializeField] private BeatlinePooler pooler;

    protected override IPooler<Beatline> Pooler => pooler;
}

using UnityEngine;

public class BeatlineLane : BaseBeatlineLane<Beatline> // BPMData is not acted upon here - any calls to it happen in base.Awake()
{
    [SerializeField] private BeatlinePooler pooler;

    protected override IPooler<Beatline> Pooler => pooler;
}

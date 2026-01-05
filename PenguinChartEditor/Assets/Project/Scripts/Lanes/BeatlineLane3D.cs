using UnityEngine;

public class BeatlineLane3D : BaseBeatlineLane<Beatline3D>
{
    [SerializeField] BeatlinePooler3D pooler;

    protected override IPooler<Beatline3D> Pooler => pooler;
}

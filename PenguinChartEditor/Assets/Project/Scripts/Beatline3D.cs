using UnityEngine;

public class Beatline3D : BaseBeatline
{    
    /// <summary>
    /// Line renderer thicknesses corresponding to each beatline type in the BeatlineType enum. 
    /// </summary>
    protected override float[] thicknesses => _thicknesses;

    float[] _thicknesses = { 0, 1, 5, 10 };

}
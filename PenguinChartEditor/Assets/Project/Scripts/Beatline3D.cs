using UnityEngine;

public class Beatline3D : BaseBeatline
{    
    /// <summary>
    /// Line renderer thicknesses corresponding to each beatline type in the BeatlineType enum. 
    /// </summary>
    protected override float[] thicknesses => _thicknesses;

    float[] _thicknesses = { 0, 0.3f, 0.1f, 0.02f };

    public void UpdateBeatlinePosition(double percentOfHighway, float highwayLength)
    {
        var zPos = (float)percentOfHighway * highwayLength;

        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector3(line.GetPosition(0).x, line.GetPosition(0).y, (float)zPos);
        newPos[1] = new Vector3(line.GetPosition(1).x, line.GetPosition(1).y, (float)zPos);
        line.SetPositions(newPos);
    }

    public override void InitializeEvent(int tick, float highwayLength)
    {

    }
}
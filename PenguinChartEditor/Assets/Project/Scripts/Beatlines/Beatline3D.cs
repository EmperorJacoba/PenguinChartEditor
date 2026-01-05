using UnityEngine;

public class Beatline3D : BaseBeatline
{
    BeatlineLane3D parentLane;
    GameInstrument parentGameInstrument => parentLane.parentGameInstrument;
    /// <summary>
    /// Line renderer thicknesses corresponding to each beatline type in the BeatlineType enum. 
    /// </summary>
    protected override float[] thicknesses => _thicknesses;

    float[] _thicknesses = { 0, 0.3f, 0.1f, 0.02f };

    public override void UpdateBeatlinePosition(double percentOfHighway, float highwayLength)
    {
        var zPos = (float)percentOfHighway * highwayLength;

        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector3(parentGameInstrument.HighwayLeftEndCoordinate + parentGameInstrument.transform.position.x, line.GetPosition(0).y, (float)zPos);
        newPos[1] = new Vector3(parentGameInstrument.HighwayRightEndCoordinate + parentGameInstrument.transform.position.x, line.GetPosition(1).y, (float)zPos);
        line.SetPositions(newPos);
    }

    public override void InitializeProperties(ILane parentLane)
    {
        this.parentLane = (BeatlineLane3D)parentLane;
    }
}
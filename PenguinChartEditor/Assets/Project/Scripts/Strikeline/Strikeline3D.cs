using UnityEngine;

public class Strikeline3D : MonoBehaviour, IStrikeline
{
    // set with GameInstrument
    public GameInstrument parentGameInstrument;

    public float GetStrikelineProportion()
    {
        return transform.localPosition.z / parentGameInstrument.HighwayLength;
    }
}
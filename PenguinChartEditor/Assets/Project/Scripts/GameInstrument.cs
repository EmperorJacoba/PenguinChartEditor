using UnityEngine;

public class GameInstrument : MonoBehaviour
{
    [SerializeField] Highway3D highway;
    [SerializeField] Strikeline3D strikeline;
    [SerializeField] Waveform waveform;

    public IInstrument representedInstrument;

    public float HighwayLength => highway.Length;
    public float GetCenterXCoordinateFromLane(int lane) => highway.GetCenterXCoordinateFromLane(lane);
    public int MatchXCoordinateToLane(float xCoordinate) => highway.MatchXCoordinateToLane(xCoordinate);
    public Vector3 GetCursorHighwayPosition() => highway.GetCursorHighwayPosition();
    public float GetCursorHighwayProportion() => highway.GetCursorHighwayProportion();
    public float GetStrikelineHighwayProportion() => strikeline.GetStrikelineProportion(); // todo: cache this

    void Start()
    {
        representedInstrument = Chart.LoadedInstrument;
    }
}
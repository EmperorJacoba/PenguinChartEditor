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

    void Awake()
    {
        representedInstrument = Chart.LoadedInstrument;
    }
}
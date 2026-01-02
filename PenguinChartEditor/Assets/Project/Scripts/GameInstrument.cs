using UnityEngine;

public class GameInstrument : MonoBehaviour
{
    public Highway3D highway;
    public IInstrument representedInstrument;

    public float GetCenterXCoordinateFromLane(int lane) => highway.GetCenterXCoordinateFromLane(lane);
    public int MatchXCoordinateToLane(float xCoordinate) => highway.MatchXCoordinateToLane(xCoordinate);
    public Vector3 GetCursorHighwayPosition() => highway.GetCursorHighwayPosition();
    public float GetCursorHighwayProportion() => highway.GetCursorHighwayProportion();
}
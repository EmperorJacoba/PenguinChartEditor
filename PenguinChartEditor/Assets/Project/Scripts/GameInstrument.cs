using UnityEngine;
using System.Linq;

public class GameInstrument : MonoBehaviour
{
    [SerializeField] Highway3D highway;
    
    // inject via strikeline script itself
    public IStrikeline strikeline;
    [SerializeField] Waveform waveform;

    [SerializeField] InstrumentType instrumentType;
    public IInstrument representedInstrument
    {
        get
        {
            if (_instRef == null)
            {
                var instrumentCandidates = Chart.Instruments.Where(item => item.InstrumentName == instrumentType).ToList();
                if (instrumentCandidates.Count == 0)
                {
                    Debug.LogError("Instrument not found in instrument database.");
                    return null;
                }
                _instRef = instrumentCandidates.First();
            }
            return _instRef;
        }
    }
    IInstrument _instRef;

    public float HighwayLength => highway.Length;
    public float GetCenterXCoordinateFromLane(int lane) => highway.GetCenterXCoordinateFromLane(lane);
    public int MatchXCoordinateToLane(float xCoordinate) => highway.MatchXCoordinateToLane(xCoordinate);
    public Vector3 GetCursorHighwayPosition() => highway.GetCursorHighwayPosition();
    public float GetCursorHighwayProportion() => highway.GetCursorHighwayProportion();
    public float HighwayLeftEndCoordinate => highway.LeftEndCoordinate;
    public float HighwayRightEndCoordinate => highway.RightEndCoordinate;
    public Vector3 HighwayTransformProperties => highway.transform.localPosition;
    public Vector3 HighwayGlobalTransformProperties => highway.transform.position;
    public Vector3 HighwayLocalScaleProperties => highway.transform.localScale;

    void Awake()
    {
//        representedInstrument = Chart.LoadedInstrument;
    }
}
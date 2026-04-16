using System;
using UnityEngine;

public class Beatline : MonoBehaviour, IPoolable
{
    private const float ORTHOGRAPHIC_VIEW_CONVERSION_FACTOR = 3.0f;
    public int Tick { get; set; } = -1;

    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            gameObject.SetActive(value);
        }
    }

    public Coroutine destructionCoroutine { get; set; }

    private void UpdateBeatlinePosition() => UpdateBeatlinePosition(Waveform.GetWaveformRatio(Tick));

    /// <summary>
    /// The line renderer attached to the beatline game object.
    /// </summary>
    private LineRenderer line;

    /// <summary>
    /// The possible types of beatlines that exist.
    /// <para>none: There is no beatline of any type at this tick with the current TS.</para>
    /// <para>barline: There is a start of a bar at this tick with the current TS.</para>
    /// <para>divisionLine: There is a first division beat at this tick with the current TS. (e.g quarter note in 4/4, eighth note in 5/8)</para>
    /// <para>halfDivisionLine: There is a second division beat at this tick with the current TS. (e.g eighth note in 4/4, sixteenth note in 5/8)</para>
    /// </summary>
    public enum BeatlineType
    {
        none = 0,
        barline = 1,
        divisionLine = 2,
        halfDivisionLine = 3
    }
    
    /// <summary>
    /// The type of beatline that this beatline object is.
    /// </summary>
    public BeatlineType Type
    {
        get { return _bt; }
        set
        {
            // enum value corresponds to index in thickness array
            UpdateThickness(value);
            _bt = value;
        }
    }

    private BeatlineType _bt = BeatlineType.none;
    
    protected void UpdateThickness(BeatlineType type)
    {
        var thickness = thicknesses[(int)type];

        if (Chart.LoadedInstrument == Chart.SyncTrackInstrument)
        {
            // Tempo Map is top-down orthographic 3D to portray 2D, so beatlines look weird using normal thicknesses. 
            // Keep same ratios but adjust the thicknesses
            thickness /= ORTHOGRAPHIC_VIEW_CONVERSION_FACTOR;
        }

        if (type == BeatlineType.none) line.enabled = false;
        else line.enabled = true;

        line.startWidth = thickness;
        line.endWidth = thickness;
    }

    public void InitializeEvent(int tick)
    {
        if (tick < 0) return;
        Tick = tick;
        UpdateBeatlinePosition(Waveform.GetWaveformRatio(Tick));
        Type = Chart.SyncTrackInstrument.CalculateBeatlineType(Tick);
    }
    
    private BeatlineLane parentLane;
    private GameInstrument parentGameInstrument => parentLane.parentGameInstrument;

    private readonly float[] thicknesses = { 0, 0.3f, 0.1f, 0.02f };

    public void UpdateBeatlinePosition(double percentOfHighway)
    {
        var zPos = (float)percentOfHighway * parentGameInstrument.HighwayLength;

        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector3(parentGameInstrument.HighwayLeftEndCoordinate + parentGameInstrument.transform.position.x, line.GetPosition(0).y, (float)zPos);
        newPos[1] = new Vector3(parentGameInstrument.HighwayRightEndCoordinate + parentGameInstrument.transform.position.x, line.GetPosition(1).y, (float)zPos);
        line.SetPositions(newPos);
    }

    public void InitializeProperties(ILane parentLane)
    {
        this.parentLane = (BeatlineLane)parentLane;
    }

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }
}
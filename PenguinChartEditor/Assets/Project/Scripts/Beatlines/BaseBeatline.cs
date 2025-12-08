using UnityEngine;

public abstract class BaseBeatline : MonoBehaviour, IPoolable
{
    protected InputMap inputMap;
    public int Tick { get; set; }

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

    public abstract void UpdateBeatlinePosition(double percentOfHighway, float highwayLength);

    #region Components

    /// <summary>
    /// The line renderer attached to the beatline game object.
    /// </summary>
    [SerializeField] protected LineRenderer line;

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
    /// Line renderer thicknesses corresponding to each beatline type in the BeatlineType enum. 
    /// </summary>
    protected abstract float[] thicknesses { get; }

    #endregion

    #region Properties

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
    BeatlineType _bt = BeatlineType.none;

    #endregion

    #region Calculators

    protected void UpdateThickness(BeatlineType type)
    {
        var thickness = thicknesses[(int)type];

        if (type == BeatlineType.none) line.enabled = false;
        else line.enabled = true; // VERY IMPORTANT OTHERWISE IT WILL NOT TURN BACK ON EVER

        line.startWidth = thickness;
        line.endWidth = thickness;
    }

    public void InitializeEvent(int tick, float highwayLength)
    {
        if (tick < 0) return;
        UpdateBeatlinePosition(Waveform.GetWaveformRatio(tick), highwayLength);
        Type = Chart.SyncTrackInstrument.CalculateBeatlineType(tick);
    }

    #endregion
}
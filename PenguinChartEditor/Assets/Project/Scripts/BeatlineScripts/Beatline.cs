using UnityEngine;

/// <summary>
/// The script attached to the beatline prefab. 
/// <para>The beatline prefab is a UI element with a line renderer with two points set to the width of the track, and has malleable BPM and TS labels.</para>
/// <remarks>Beatline game object control should happen through this class.</remarks>
/// </summary>
public class Beatline : MonoBehaviour
{
    protected InputMap inputMap;
    public int Tick
    {
        get { return _tick; }
        set
        {
            _tick = value;
        }
    }

    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            if (!value) { destructionCoroutine = BeatlinePooler.instance.StartCoroutine(BeatlinePooler.instance.DestructionTimer(this)); }
            else BeatlinePooler.instance.StopCoroutine(destructionCoroutine);
            gameObject.SetActive(value);
        }
    }

    Coroutine destructionCoroutine;

    private int _tick = 0;

    #region Components

    //[SerializeField] protected BPM bpmLabel;
    //[SerializeField] protected TimeSignature tsLabel;
    //[SerializeField] protected Warning tsWarningAlert; // make own warning script in future, maybe have CreateNewWarning() for tooltips??
    //[SerializeField] protected RectTransform tsWarningAlertRectTransform;

    /// <summary>
    /// The line renderer attached to the beatline game object.
    /// </summary>
    [SerializeField] LineRenderer line;

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
    float[] thicknesses = { 0, 0.05f, 0.02f, 0.005f };

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
            UpdateThickness(value);
            _bt = value;
        }
    }
    BeatlineType _bt = BeatlineType.none;

    private float yScreenProportion = 0;

    #endregion

    #region Functions

    /// <summary>
    /// Update the position of the beatline to a specified proportion up the screen.
    /// </summary>
    /// <param name="percentOfScreen">The percent of the screen that should exist between the bottom and the beatline.</param>
    public void UpdateBeatlinePosition(double percentOfScreen, float screenHeight)
    {
        // use screen ref to calculate percent of screen -> scale is 1:1 in the line renderer (scale must be 1, 1, 1)
        yScreenProportion = (float)(percentOfScreen * screenHeight);

        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector2(line.GetPosition(0).x, (float)yScreenProportion);
        newPos[1] = new Vector2(line.GetPosition(1).x, (float)yScreenProportion);
        line.SetPositions(newPos);

    }

    #endregion

    #region Calculators

    private void UpdateThickness(BeatlineType type)
    {
        var thickness = thicknesses[(int)type];

        if (type == BeatlineType.none) line.enabled = false;
        else line.enabled = true; // VERY IMPORTANT OTHERWISE IT WILL NOT TURN BACK ON EVER

        line.startWidth = thickness;
        line.endWidth = thickness;
    }

    #endregion
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The script attached to the beatline prefab. 
/// <para>The beatline prefab is a UI element with a line renderer with two points set to the width of the track.</para>
/// <remarks>Beatline game object control should happen through this class.</remarks>
/// </summary>
public class Beatline : MonoBehaviour
{
    #region Components

    /// <summary>
    /// The BPM label text on the beatline's label.
    /// </summary>
    private TextMeshProUGUI bpmLabelText;

    /// <summary>
    /// The container for the label object and the label text.
    /// </summary>
    private GameObject bpmLabel;
    private RectTransform bpmLabelRt;

    private GameObject tsLabel;
    private RectTransform tsLabelRt;
    private TextMeshProUGUI tsLabelText;

    /// <summary>
    /// The line renderer attached to the beatline game object.
    /// </summary>
    public LineRenderer line;


    private RectTransform screenRefRect;

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
    float[] thicknesses = {0, 0.05f, 0.02f, 0.005f};

    #endregion
    #region Properties

    /// <summary>
    /// Is the beatline currently visible?
    /// </summary>
    public bool IsVisible
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

    /// <summary>
    /// Is the BPM label currently visible?
    /// </summary>
    public bool BPMLabelVisible
    {
        get
        {
            return bpmLabel.activeInHierarchy;
        }
        set
        {
            bpmLabel.SetActive(value);
            UpdateLabelPositions();
        }
    }

    /// <summary>
    /// The text shown by the BPM label.
    /// </summary>
    public string BPMLabelText
    {
        get
        {
            return bpmLabelText.text;
        }
        set
        {
            BPMLabelVisible = true;
            bpmLabelText.text = value;
        }
    }

    public bool TSLabelVisible
    {
        get
        {
            return tsLabel.activeInHierarchy;
        }
        set
        {
            tsLabel.SetActive(value);
            UpdateLabelPositions();
        }
    }

    public string TSLabelText
    {
        get
        {
            return tsLabelText.text;
        }
        set
        {
            BPMLabelVisible = true;
            tsLabelText.text = value;
        }
    }

    /// <summary>
    /// The type of beatline that this beatline object is.
    /// </summary>
    public BeatlineType Type
    {
        get { return _bt; }
        set
        {
            if (value == _bt) return;
            UpdateThickness(value);
            _bt = value;
        }
    }
    BeatlineType _bt = BeatlineType.none;

    #endregion
    #region Functions 

    /// <summary>
    /// Update the position of the beatline to a specified proportion up the screen.
    /// </summary>
    /// <param name="percentOfScreen">The percent of the screen that should exist between the bottom and the beatline.</param>
    public void UpdateBeatlinePosition(double percentOfScreen) // change this to percentage later
    {
        // use screen ref to calculate percent of screen -> scale is 1:1 in the line renderer (scale must be 1, 1, 1)
        var newYPos = percentOfScreen * screenRefRect.rect.height;

        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector2(line.GetPosition(0).x, (float)newYPos);
        newPos[1] = new Vector2(line.GetPosition(1).x, (float)newYPos);
        line.SetPositions(newPos);

        UpdateLabelPositions(); // to keep the labels locked to their beatlines
    }
    
    #endregion
    
    #region Internal Functions
    private void UpdateLabelPositions()
    {
        bpmLabel.transform.localPosition = new Vector3(bpmLabel.transform.localPosition.x, line.GetPosition(1).y - (bpmLabelRt.rect.height / 2));
        tsLabel.transform.localPosition = new Vector3(tsLabel.transform.localPosition.x, line.GetPosition(0).y - (tsLabelRt.rect.height / 2));
    }

    private void UpdateThickness(BeatlineType type)
    {
        var thickness = thicknesses[(int)type];

        if (type == BeatlineType.none) line.enabled = false;
        else line.enabled = true; // VERY IMPORTANT OTHERWISE IT WILL NOT TURN BACK ON EVER

        line.startWidth = thickness;
        line.endWidth = thickness;
    }

    void Awake()
    {
        screenRefRect = GameObject.Find("ScreenReference").GetComponent<RectTransform>();

        bpmLabel = transform.GetChild(0).gameObject;
        bpmLabelRt = bpmLabel.GetComponent<RectTransform>();
        bpmLabelText = bpmLabel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        tsLabel = transform.GetChild(1).gameObject;
        tsLabelRt = tsLabel.GetComponent<RectTransform>();
        tsLabelText = bpmLabel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        line = gameObject.GetComponent<LineRenderer>();
    }

    void Start()
    {
        BPMLabelVisible = false;
    }

    #endregion
}

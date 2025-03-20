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
    private TextMeshProUGUI bpmLabel;

    /// <summary>
    /// The container for the label object and the label text.
    /// </summary>
    private GameObject beatlineLabel;
    private RectTransform beatlineLabelRt;

    /// <summary>
    /// The line renderer attached to the beatline game object.
    /// </summary>
    public LineRenderer line;

    /// <summary>
    /// The RectTransform attached to the beatline game object.
    /// </summary>
    private RectTransform beatlineRt;

    private RectTransform screenRefRect;

    public enum BeatlineType
    {
        none = 0,
        barline = 1,
        divisionLine = 2,
        halfDivisionLine = 3
    }

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
            return beatlineLabel.activeInHierarchy;
        }
        set
        {
            beatlineLabel.SetActive(value);
            UpdateLabel();
        }
    }

    /// <summary>
    /// The text shown by the BPM label.
    /// </summary>
    public string BPMLabelText
    {
        get
        {
            return bpmLabel.text;
        }
        set
        {
            bpmLabel.text = value;
        }
    }

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
        UpdateLabel();
    }
    
    #endregion
    
    #region Internal Functions
    private void UpdateLabel()
    {
        beatlineLabel.transform.localPosition = new Vector3(beatlineLabel.transform.localPosition.x, line.GetPosition(1).y - (beatlineLabelRt.rect.height / 2));
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
        beatlineRt = gameObject.GetComponent<RectTransform>();
        beatlineLabel = transform.GetChild(0).gameObject;
        beatlineLabelRt = beatlineLabel.GetComponent<RectTransform>();
        bpmLabel = beatlineLabel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        line = gameObject.GetComponent<LineRenderer>();
    }

    void Start()
    {
        BPMLabelVisible = false;
    }

    #endregion
}

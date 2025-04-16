using System;
using System.Diagnostics;
using TMPro;
using UnityEngine;

/// <summary>
/// The script attached to the beatline prefab. 
/// <para>The beatline prefab is a UI element with a line renderer with two points set to the width of the track.</para>
/// <remarks>Beatline game object control should happen through this class.</remarks>
/// </summary>
public class Beatline : MonoBehaviour
{
    #region Components

    [SerializeField] bool isPreviewBeatline;

    [SerializeField] GameObject bpmLabel;
    [SerializeField] RectTransform bpmLabelRt;
    [SerializeField] TMP_InputField bpmLabelEntryBox;
    [SerializeField] TextMeshProUGUI bpmLabelText;

    [SerializeField] GameObject tsLabel;
    [SerializeField] RectTransform tsLabelRt;
    [SerializeField] TMP_InputField tsLabelEntryBox;
    [SerializeField] TextMeshProUGUI tsLabelText;

    /// <summary>
    /// The line renderer attached to the beatline game object.
    /// </summary>
    [SerializeField] LineRenderer line;

    RectTransform screenRefRect;

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

    public int HeldTick { get; set; } = 0;
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
            UpdateLabels();
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
            UpdateLabels();
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
            TSLabelVisible = true;
            tsLabelText.text = value;
        }
    }

    public BeatlinePreviewer.PreviewType EditType
    {
        set
        {
            switch (value)
            {
                case BeatlinePreviewer.PreviewType.none:
                    tsLabelEntryBox.gameObject.SetActive(false);
                    bpmLabelEntryBox.gameObject.SetActive(false);
                    break;
                case BeatlinePreviewer.PreviewType.BPM:
                    bpmLabelEntryBox.gameObject.SetActive(true);
                    tsLabelEntryBox.gameObject.SetActive(false);

                    bpmLabelEntryBox.ActivateInputField();
                    bpmLabelEntryBox.text = SongTimelineManager.TempoEvents[HeldTick].Item1.ToString();
                    BeatlinePreviewer.editMode = false;
                    break;
                case BeatlinePreviewer.PreviewType.TS:
                    tsLabelEntryBox.gameObject.SetActive(true);
                    bpmLabelEntryBox.gameObject.SetActive(false);

                    tsLabelEntryBox.ActivateInputField();
                    BeatlinePreviewer.editMode = false;
                    tsLabelEntryBox.text = $"{SongTimelineManager.TimeSignatureEvents[HeldTick].Item1} / {SongTimelineManager.TimeSignatureEvents[HeldTick].Item2}";
                    break;
            }
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

        UpdateLabels(); // to keep the labels locked to their beatlines
    }
    
    #endregion
    
    #region Internal Functions
    private void UpdateLabels()
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
    }

    void Start()
    {
        BPMLabelVisible = false;
        if (!isPreviewBeatline)
        {
            bpmLabelEntryBox.onEndEdit.AddListener(x => HandleBPMEndEdit(x));
            tsLabelEntryBox.onEndEdit.AddListener(x => HandleTSEndEdit(x));
        }
    }

    public void CheckForEvents()
    {
        if (SongTimelineManager.TempoEvents.ContainsKey(HeldTick))
        {
            BPMLabelVisible = true;
            BPMLabelText = $"{SongTimelineManager.TempoEvents[HeldTick].Item1}";
        }
        else
        {
            BPMLabelVisible = false;
        }

        if (SongTimelineManager.TimeSignatureEvents.ContainsKey(HeldTick))
        {
            TSLabelVisible = true;
            TSLabelText = $"{SongTimelineManager.TimeSignatureEvents[HeldTick].Item1} / {SongTimelineManager.TimeSignatureEvents[HeldTick].Item2}";
        }
        else
        {
            TSLabelVisible = false;
        }

        if (HeldTick == BeatlinePreviewer.focusedTick.Item1) EditType = BeatlinePreviewer.focusedTick.Item2;
        else EditType = BeatlinePreviewer.PreviewType.none;
    }
    #endregion

    public void HandleBPMEndEdit(string newBPM)
    {
        SongTimelineManager.TempoEvents[HeldTick] = (ProcessUnsafeBPMString(newBPM), SongTimelineManager.TempoEvents[HeldTick].Item2);
        SongTimelineManager.RecalculateTempoEventDictionary(HeldTick);

        BeatlinePreviewer.focusedTick = (0, BeatlinePreviewer.PreviewType.none);
        BeatlinePreviewer.editMode = true;

        bpmLabelEntryBox.gameObject.SetActive(false);
        TempoManager.UpdateBeatlines();
    }

    public void HandleBPMDeselect()
    {
        BeatlinePreviewer.focusedTick = (0, BeatlinePreviewer.PreviewType.none);
        bpmLabelEntryBox.gameObject.SetActive(false);
        TempoManager.UpdateBeatlines();
    }

    public void HandleTSEndEdit(string newTS)
    {
        SongTimelineManager.TimeSignatureEvents[HeldTick] = (ProcessUnsafeTS(newTS));
        BeatlinePreviewer.focusedTick = (0, BeatlinePreviewer.PreviewType.none);
        BeatlinePreviewer.editMode = true;
        tsLabelEntryBox.gameObject.SetActive(false);   
        TempoManager.UpdateBeatlines();
    }

    public void HandleTSDeselect()
    {
        BeatlinePreviewer.focusedTick = (0, BeatlinePreviewer.PreviewType.none);
        tsLabelEntryBox.gameObject.SetActive(false);
        TempoManager.UpdateBeatlines();
    }

    float ProcessUnsafeBPMString(string newBPM)
    {
        var bpmAsFloat = float.Parse(newBPM);
        if (bpmAsFloat == 0 || bpmAsFloat > 1000.0f)
        {
            return SongTimelineManager.TempoEvents[HeldTick].Item1;
        }
        bpmAsFloat = (float)Math.Round(bpmAsFloat, 3);
        return bpmAsFloat;
    }

    (int, int) ProcessUnsafeTS(string newTS)
    {
        var currentTS = SongTimelineManager.TimeSignatureEvents[HeldTick];
        var seperatedTS = newTS.Split("/");
        if (seperatedTS.Length == 1) return currentTS;

        int num;
        if (!int.TryParse(seperatedTS[0], out num)) return currentTS;

        int denom;
        if (!int.TryParse(seperatedTS[1], out denom)) return currentTS;
        if (!(denom != 0 && (denom & (denom - 1)) == 0)) return currentTS; // taken from https://stackoverflow.com/questions/600293/how-to-check-if-a-number-is-a-power-of-2 

        return (num, denom);
    }
}

// Add code to handle text box actions and committing changes to dictionary
// Edit by typing
// Edit by dragging
// Edit by adding
// Edit by deleting
// Anchors
// Saving
// Keyybindsa

// edit mode bool flipping will not work right right now
// get better solution

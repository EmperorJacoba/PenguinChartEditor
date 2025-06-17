using System;
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

    /// <summary>
    /// The tick that this beatline object represents.
    /// </summary>
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

    /// <summary>
    /// Is the TS label currently visible?
    /// </summary>
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

    /// <summary>
    /// The text shown by the TS label. 
    /// </summary>
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

    /// <summary>
    /// Is the pointer in this beatline's BPM label?
    /// <para>This is controlled by an event handler attached to the BPM object nested under the Beatline game object.</para>
    /// </summary>
    public bool pointerInBPMLabel {get; set;}

    /// <summary>
    /// Is the pointer currently in this beatline's TS label?
    /// <para>This is controlled by an event handler attached to the TS object nested under the Beatline game object.</para>
    /// </summary>
    public bool pointerInTSLabel {get; set;}

    /// <summary>
    /// Set up input fields to display and activate by passing in the type of label to edit.
    /// </summary>
    public BeatlinePreviewer.PreviewType EditType
    {
        set
        {
            if (!isPreviewBeatline)
            {
                switch (value)
                {
                    case BeatlinePreviewer.PreviewType.none:
                        tsLabelEntryBox.gameObject.SetActive(false);
                        bpmLabelEntryBox.gameObject.SetActive(false);

                        SongTimelineManager.EnableChartingInputMap();
                        break;
                    case BeatlinePreviewer.PreviewType.BPM:
                        bpmLabelEntryBox.gameObject.SetActive(true);
                        tsLabelEntryBox.gameObject.SetActive(false);

                        bpmLabelEntryBox.ActivateInputField();

                        // I am bewildered as to why this activates for beatlines that
                        // don't have tick entries in TempoEvents, on the same exact frame as
                        // another label being selected, when that should not be possible, because
                        // a) beatlines without entries do not have labels and thus cannot access this variable (never edited)
                        // b) this is activated when a pointer is in a label and clicked, and no other way, which should not be true for more than one beatline
                        // sooo this is here to keep it from throwing an error when trying to access a nonexistent tick (which, again, should not be possible)
                        // i have no idea why this happens and it doesn't break everything if i just check it here
                        // please someone more intelligent than me fix this weird as heck error
                        try 
                        {
                            bpmLabelEntryBox.text = SongTimelineManager.TempoEvents[HeldTick].Item1.ToString();
                        }
                        catch
                        {
                            return;
                        }
                        BeatlinePreviewer.editMode = false;

                        SongTimelineManager.DisableChartingInputMap();
                        break;
                    case BeatlinePreviewer.PreviewType.TS:
                        tsLabelEntryBox.gameObject.SetActive(true);
                        bpmLabelEntryBox.gameObject.SetActive(false);

                        tsLabelEntryBox.ActivateInputField();
                        BeatlinePreviewer.editMode = false;

                        // see above comment over BPM equivilent
                        try
                        {
                            tsLabelEntryBox.text = $"{SongTimelineManager.TimeSignatureEvents[HeldTick].Item1} / {SongTimelineManager.TimeSignatureEvents[HeldTick].Item2}";
                        }
                        catch
                        {
                            return;
                        }

                        SongTimelineManager.DisableChartingInputMap();
                        break;
                }
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
    public void UpdateBeatlinePosition(double percentOfScreen)
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

    float originalMouseY = float.NaN;

    /// <summary>
    /// Change the data associated with a beatline event based on a click + drag from the user.
    /// </summary>
    /// <param name="currentMouseY">The current mouse position.</param>
    private void ChangeBeatlinePositionFromDrag(float currentMouseY)
    {
        WaveformManager.GetCurrentDisplayedWaveformInfo(out var _, out var _, out var timeShown, out var _, out var _);

        // Calculate the amount of time that the mouse has traversed in the last frame
        var mouseDelta = currentMouseY - originalMouseY; // Original Y comes from the previous frame. If it came from the start of the drag, the timeChange addition to newTime would stack.
        var percentOfScreenMoved = mouseDelta / screenRefRect.rect.height;
        var timeChange = percentOfScreenMoved * timeShown;

        // Use exclusive function because this needs to find the tempo event before this beatline's tempo event.
        // Inclusive would always return the same event, which causes to be 0/0 and thus NaN.
        var lastBPMTick = SongTimelineManager.FindLastTempoEventTickExclusive(HeldTick);

        var newTime = SongTimelineManager.TempoEvents[HeldTick].Item2 + (float)timeChange;

        // time is measured in seconds so this is beats per second, multiply by 60 to convert to BPM
        // Calculate the new BPM based on the time change
        float newBPS = ((HeldTick - lastBPMTick) / (float)ChartMetadata.ChartResolution) / (newTime - SongTimelineManager.TempoEvents[lastBPMTick].Item2);
        float newBPM = (float)Math.Round((newBPS * 60), 3);

        if (newBPM < 0 || newBPM > 1000) return; // BPM can't be negative and event selection gets screwed with when the BPM is too high

        // Write new data - time changes for this beatline's tick, BPM changes for the last tick event.
        SongTimelineManager.TempoEvents[HeldTick] = (SongTimelineManager.TempoEvents[HeldTick].Item1, newTime);
        SongTimelineManager.TempoEvents[lastBPMTick] = (newBPM, SongTimelineManager.TempoEvents[lastBPMTick].Item2);

        // Update rest of dictionary to account for the time change.
        SongTimelineManager.RecalculateTempoEventDictionary(HeldTick, (float)timeChange);

        // Display the changes
        TempoManager.UpdateBeatlines();
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

    static int activeDragTick;
    private void Update() 
    {
        if (!CheckForActiveDragEdits())
        {
            if (pointerInBPMLabel && Input.GetMouseButtonDown(0))
            {
                EditType = BeatlinePreviewer.PreviewType.BPM;
            }
            else if (pointerInTSLabel && Input.GetMouseButtonDown(0))
            {
                EditType = BeatlinePreviewer.PreviewType.TS;
            }
        }

        // Reset active drag tick to inactive state if either pair of the drag keybind is lifted
        if (!Input.GetKey(KeyCode.LeftControl) || !Input.GetMouseButton(0))
        {
            activeDragTick = -1;
            originalMouseY = float.NaN;
        }
    }
    
    /// <summary>
    /// Runs every frame to check if the user is making a change to BPM via control+drag.
    /// </summary>
    /// <returns>Is the user making changes to a BPM label via dragging?</returns>
    bool CheckForActiveDragEdits()
    {
        // For some reason the pointing logic isn't 100% accurate and will select a beatline
        // w/o a flag (and thus without an event, which cannot be dragged).
        // No idea why it does this, but the dict check prevents this logic from trying to change a nonreal event in the TempoEvents dictionary.
        // This really shouldn't be an issue that needs to be checked for, but here we are.
        // I don't know what I'm overlooking to cause it. Confusion hath overtaken me.
        // tick timestamp 0 cannot be dragged (because there is no prior BPM to modify)
        if (!SongTimelineManager.TempoEvents.ContainsKey(HeldTick) || HeldTick == 0) return false;

        // I'm using old input system for this b/c it's easier to implement with the individual BPM events
        // Check if the user is trying to make an edit (LCTRL + Click) and if they're over a label. 
        // activeDragTick stores the tick for the beatline that is being dragged. 
        // Without it, this can fire for multiple ticks at the same time
        // (I don't know how it's possible, because pointerInBPMLabel should only be true for one flag, but it will return true for two events w/o it)
        // activeDragTick also (primarily) allows a beatline to continue being dragged even if the pointer leaves the BPM label (which is common when moving up & down within the same drag)
        if (activeDragTick == -1 && pointerInBPMLabel && Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(0))
        {
            activeDragTick = HeldTick;

            if (float.IsNaN(originalMouseY)) // First frame of an edit, show delta to be 0
            {
                originalMouseY = Input.mousePosition.y;
            }

            ChangeBeatlinePositionFromDrag(Input.mousePosition.y);
            return true;
        }
        // Once the drag starts, this check will run instead because activeDragTick will be initialized.
        else if (activeDragTick == HeldTick && Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(0))
        {
            ChangeBeatlinePositionFromDrag(Input.mousePosition.y);
            originalMouseY = Input.mousePosition.y;
            return true;
        }
        return false;

        // Change pointerInBPMLabel event trigger to be based on pointer click instead of enter?
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

        // Check to see if this tick is being edited.
        // This does have the side effect of wiping input data upon a scroll. Oops
        // if (HeldTick == BeatlinePreviewer.focusedTick.Item1) EditType = BeatlinePreviewer.focusedTick.Item2;
        // else EditType = BeatlinePreviewer.PreviewType.none;
    }
    #endregion

    public void HandleBPMEndEdit(string newBPM)
    {
        try 
        {
            SongTimelineManager.TempoEvents[HeldTick] = (ProcessUnsafeBPMString(newBPM), SongTimelineManager.TempoEvents[HeldTick].Item2);
        }
        catch 
        {
            return;
        }

        SongTimelineManager.RecalculateTempoEventDictionary(HeldTick);

        BeatlinePreviewer.editMode = true;
        ConcludeBPMEdit();
    }

    public void HandleBPMDeselect()
    {
        ConcludeBPMEdit();
    }

    public void HandleTSEndEdit(string newTS)
    {
        SongTimelineManager.TimeSignatureEvents[HeldTick] = (ProcessUnsafeTS(newTS));
        BeatlinePreviewer.editMode = true;
        ConcludeTSEdit();
    }

    public void HandleTSDeselect()
    {
        ConcludeTSEdit();
    }

    void ConcludeBPMEdit()
    {
        bpmLabelEntryBox.gameObject.SetActive(false);
        TempoManager.UpdateBeatlines();
        EditType = BeatlinePreviewer.PreviewType.none;
    }

    void ConcludeTSEdit()
    {
        tsLabelEntryBox.gameObject.SetActive(false);
        TempoManager.UpdateBeatlines();
        EditType = BeatlinePreviewer.PreviewType.none;
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

        if (!int.TryParse(seperatedTS[0], out int num)) return currentTS; // dunno why this would fail but i have a feeling

        if (!int.TryParse(seperatedTS[1], out int denom)) return currentTS;
        // TS denoms are only valid as a power of 2 (1, 2, 4, 8, etc.)
        if (!(denom != 0 && (denom & (denom - 1)) == 0)) return currentTS; // taken from https://stackoverflow.com/questions/600293/how-to-check-if-a-number-is-a-power-of-2 

        return (num, denom);
    }
}
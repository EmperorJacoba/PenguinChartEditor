using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

/// <summary>
/// The script attached to the beatline prefab. 
/// <para>The beatline prefab is a UI element with a line renderer with two points set to the width of the track.</para>
/// <remarks>Beatline game object control should happen through this class.</remarks>
/// </summary>
public class Beatline : MonoBehaviour
{
    #region Components

    InputMap inputMap;

    /// <summary>
    /// Property used to turn off some editing features not available for the "preview" beatline object.
    /// </summary>
    [SerializeField] bool isPreviewBeatline;

    [SerializeField] GameObject bpmLabel;
    [SerializeField] RectTransform bpmLabelRt;
    [SerializeField] TMP_InputField bpmLabelEntryBox;
    [SerializeField] TextMeshProUGUI bpmLabelText;

    [SerializeField] GameObject tsLabel;
    [SerializeField] RectTransform tsLabelRt;
    [SerializeField] TMP_InputField tsLabelEntryBox;
    [SerializeField] TextMeshProUGUI tsLabelText;

    [SerializeField] GameObject bpmSelectionOverlay;
    [SerializeField] GameObject tsSelectionOverlay;

    /// <summary>
    /// The line renderer attached to the beatline game object.
    /// </summary>
    [SerializeField] LineRenderer line;

    RectTransform screenRef;

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
    /// The tick that this beatline object represents.
    /// </summary>
    public int HeldTick { get; set; } = 0;

    /// <summary>
    /// Holds a reference for all the BPM events currently selected.
    /// </summary>
    public static HashSet<int> SelectedBPMTicks { get; set; } = new();

    /// <summary>
    /// Holds a reference for all the TS events currently selected.
    /// </summary>
    public static HashSet<int> SelectedTSTicks { get; set; } = new();

    /// <summary>
    /// Holds the tick that was last interacted with in a selection. Used for shift-click capabilities.
    /// </summary>
    public static int lastTickSelection = 0;

    /// <summary>
    /// Is the BPM event attached to this beatline currently selected?
    /// <para>Selection overlay/indicator is triggered when the value of this property is set.</para>
    /// </summary>
    public bool BpmSelected
    {
        get
        {
            return _bpmSelected;
        }
        set
        {
            bpmSelectionOverlay.SetActive(value);
            _bpmSelected = value;
        }
    }
    bool _bpmSelected = false;

    /// <summary>
    /// Is the TS event attached to this beatline currently selected?
    /// <para>Selection overlay/indicator is triggered when the value of this property is set.</para>
    /// </summary>
    public bool TsSelected
    {
        get
        {
            return _tsSelected;
        }
        set
        {
            tsSelectionOverlay.SetActive(value);
            _tsSelected = value;
        }
    }
    bool _tsSelected = false;

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
            UpdateLabelPosition();
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
            UpdateLabelPosition();
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
        var newYPos = percentOfScreen * screenRef.rect.height;

        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector2(line.GetPosition(0).x, (float)newYPos);
        newPos[1] = new Vector2(line.GetPosition(1).x, (float)newYPos);
        line.SetPositions(newPos);

        UpdateLabelPosition(); // to keep the labels locked to their beatlines
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

        BpmSelected = CheckForBPMSelected();
        TsSelected = CheckForTSSelected();
    }

    #endregion

    #region Calculators

    private void UpdateLabelPosition()
    {
        bpmLabel.transform.localPosition = new Vector3(bpmLabel.transform.localPosition.x, line.GetPosition(1).y - (bpmLabelRt.rect.height / 2));
        tsLabel.transform.localPosition = new Vector3(tsLabel.transform.localPosition.x, line.GetPosition(0).y - (tsLabelRt.rect.height / 2));
    }

    /// <summary>
    /// Change the data associated with a beatline event based on a click + drag from the user.
    /// </summary>
    /// <param name="mouseDelta">The difference between the mouse on this frame versus the last frame.</param>
    private void ChangeBeatlinePositionFromDrag(float mouseDelta)
    {
        WaveformManager.GetCurrentDisplayedWaveformInfo(out var _, out var _, out var timeShown, out var _, out var _);

        var percentOfScreenMoved = mouseDelta / screenRef.rect.height;
        var timeChange = percentOfScreenMoved * timeShown;

        // Use exclusive function because this needs to find the tempo event before this beatline's tempo event.
        // Inclusive would always return the same event, which causes 0/0 and thus NaN.
        var lastBPMTick = SongTimelineManager.FindLastTempoEventTickExclusive(HeldTick);

        var newTime = SongTimelineManager.TempoEvents[HeldTick].Item2 + (float)timeChange;

        // time is measured in seconds so this is beats per second, multiply by 60 to convert to BPM
        // Calculate the new BPM based on the time change
        float newBPS = ((HeldTick - lastBPMTick) / (float)ChartMetadata.ChartResolution) / (newTime - SongTimelineManager.TempoEvents[lastBPMTick].Item2);
        float newBPM = (float)Math.Round((newBPS * 60), 3);

        if (newBPM < 0 || newBPM > 1000) return; // BPM can't be negative and event selection gets screwed with when the BPM is too high

        // Write new data: time changes for this beatline's tick, BPM changes for the last tick event.
        SongTimelineManager.TempoEvents[HeldTick] = (SongTimelineManager.TempoEvents[HeldTick].Item1, newTime);
        SongTimelineManager.TempoEvents[lastBPMTick] = (newBPM, SongTimelineManager.TempoEvents[lastBPMTick].Item2);

        // Update rest of dictionary to account for the time change.
        SongTimelineManager.RecalculateTempoEventDictionary(HeldTick, (float)timeChange);

        // Display the changes
        TempoManager.UpdateBeatlines();
    }

    /// <summary>
    /// Calculate the event(s) to be selected based on the last click event.
    /// </summary>
    /// <param name="clickButton">PointerEventData.button</param>
    /// <param name="targetSelectionSet">The selection hash set that contains this event type's selection data.</param>
    /// <param name="targetEventSet">The keys of a sorted dictionary that holds event data (beatlines, TS, etc)</param>
    void CalculateSelectionStatus(PointerEventData.InputButton clickButton, HashSet<int> targetSelectionSet, List<int> targetEventSet)
    {
        // Goal is to follow standard selection functionality of most productivity programs

        if (clickButton != PointerEventData.InputButton.Left) return;

        // Shift-click functionality
        if (Input.GetKey(KeyCode.LeftShift))
        {
            SelectedBPMTicks.Clear();
            SelectedBPMTicks.Clear();

            var minNum = Math.Min(lastTickSelection, HeldTick);
            var maxNum = Math.Max(lastTickSelection, HeldTick);
            HashSet<int> selectedEvents = targetEventSet.Where(x => x <= maxNum && x >= minNum).ToHashSet();
            targetSelectionSet.UnionWith(selectedEvents);
        }
        // Left control if item is already selected
        else if (Input.GetKey(KeyCode.LeftControl) && targetSelectionSet.Contains(HeldTick))
        {
            targetSelectionSet.Remove(HeldTick);
            return; // prevent lastTickSelection from being stored as an unselected number
        }
        // Left control if item is not currently selected
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            targetSelectionSet.Add(HeldTick);
        }
        // Regular click, no extra significant keybinds
        else
        {
            SelectedBPMTicks.Clear();
            SelectedTSTicks.Clear();
            targetSelectionSet.Add(HeldTick);
        }
        // Record the last selection data for shift-click selection
        lastTickSelection = HeldTick;
    }

    bool CheckForBPMSelected()
    {
        if (SelectedBPMTicks.Contains(HeldTick)) return true;
        else return false;
    }

    bool CheckForTSSelected()
    {
        if (SelectedTSTicks.Contains(HeldTick)) return true;
        else return false;
    }

    private void UpdateThickness(BeatlineType type)
    {
        var thickness = thicknesses[(int)type];

        if (type == BeatlineType.none) line.enabled = false;
        else line.enabled = true; // VERY IMPORTANT OTHERWISE IT WILL NOT TURN BACK ON EVER

        line.startWidth = thickness;
        line.endWidth = thickness;
    }

    #endregion

    #region Unity Functions

    void Awake()
    {
        screenRef = GameObject.Find("ScreenReference").GetComponent<RectTransform>();

        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.Delete.performed += x => DeleteSelection();
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

    #endregion

    #region Event Handlers

    void DeleteSelection()
    {
        if (SelectedBPMTicks.Count != 0)
        {
            var earliestTick = SelectedBPMTicks.Min();
            foreach (var tick in SelectedBPMTicks)
            {
                if (tick != 0)
                {
                    SongTimelineManager.TempoEvents.Remove(tick);
                }
            }
            SongTimelineManager.RecalculateTempoEventDictionary(SongTimelineManager.FindLastTempoEventTickInclusive(earliestTick));
        }
        if (SelectedTSTicks.Count != 0)
        {
            foreach (var tick in SelectedTSTicks)
            {
                if (tick != 0)
                {
                    SongTimelineManager.TimeSignatureEvents.Remove(tick);
                }
            }
        }
        SelectedBPMTicks.Clear();
        SelectedTSTicks.Clear();

        TempoManager.UpdateBeatlines();
    }

    bool bpmDeletePrimed = false;
    bool tsDeletePrimed = false;

    public void HandlBPMPointerDown(BaseEventData baseEventData)
    {
        var pointerData = (PointerEventData)baseEventData;
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            bpmDeletePrimed = true;
        }
    }

    public void HandleBPMPointerUp(BaseEventData baseEventData)
    {
        var pointerData = (PointerEventData)baseEventData;
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            bpmDeletePrimed = false;
        }
    }

    public void HandleTSPointerDown(BaseEventData baseEventData)
    {
        var pointerData = (PointerEventData)baseEventData;
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            tsDeletePrimed = true;
        }
    }

    public void HandleTSPointerUp(BaseEventData baseEventData)
    {
        var pointerData = (PointerEventData)baseEventData;
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            tsDeletePrimed = false;
        }
    }

    /// <summary>
    /// Called by the event trigger on the BPM label when the label is clicked.
    /// </summary>
    /// <param name="data"></param>
    public void HandleBPMLabelClick(BaseEventData data)
    {
        var clickdata = (PointerEventData)data;

        CalculateSelectionStatus(clickdata.button, SelectedBPMTicks, SongTimelineManager.TempoEvents.Keys.ToList());

        // Double click functionality for manual entry of beatline number
        if (!Input.GetKey(KeyCode.LeftControl) && clickdata.button == PointerEventData.InputButton.Left && clickdata.clickCount == 2)
        {
            EditType = BeatlinePreviewer.PreviewType.BPM;
        }

        if (bpmDeletePrimed && clickdata.button == PointerEventData.InputButton.Left) DeleteSelection();

        TempoManager.UpdateBeatlines();
    }

    /// <summary>
    /// Called by the event trigger on the TS label when the label is clicked.
    /// </summary>
    /// <param name="data"></param>
    public void HandleTSLabelClick(BaseEventData data)
    {
        var clickdata = (PointerEventData)data;

        CalculateSelectionStatus(clickdata.button, SelectedTSTicks, SongTimelineManager.TimeSignatureEvents.Keys.ToList());

        // Double click functionality for manual entry of time signature
        if (!Input.GetKey(KeyCode.LeftControl) && clickdata.button == PointerEventData.InputButton.Left && clickdata.clickCount == 2)
        {
            EditType = BeatlinePreviewer.PreviewType.TS;
        }

        if (tsDeletePrimed && clickdata.button == PointerEventData.InputButton.Left) DeleteSelection();

        TempoManager.UpdateBeatlines();
    }

    /// <summary>
    /// Called by the event trigger on the BPM event when a drag is registered
    /// </summary>
    /// <param name="data"></param>
    public void HandleBPMDragEvent(BaseEventData data)
    {
        var clickdata = (PointerEventData)data;

        if (HeldTick == 0) return;
        if (!Input.GetKey(KeyCode.LeftControl)) return;

        ChangeBeatlinePositionFromDrag(clickdata.delta.y);
    }

    /// <summary>
    /// Called when the BPM input field is submitted.
    /// </summary>
    /// <param name="newBPM"></param>
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

    /// <summary>
    /// Called when the BPM input field is deselected.
    /// </summary>
    public void HandleBPMDeselect()
    {
        ConcludeBPMEdit();
    }

    /// <summary>
    /// Called when the TS input field is submitted.
    /// </summary>
    /// <param name="newTS"></param>
    public void HandleTSEndEdit(string newTS)
    {
        SongTimelineManager.TimeSignatureEvents[HeldTick] = ProcessUnsafeTSString(newTS);
        BeatlinePreviewer.editMode = true;
        ConcludeTSEdit();
    }

    /// <summary>
    /// Called when the TS input field is deselected.
    /// </summary>
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

    #endregion

    #region Validators

    /// <summary>
    /// Take a BPM string generated by user and turn it into a dictionary-safe float value
    /// </summary>
    /// <param name="newBPM"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Take a TS string generated by user and turn it into a dictionary safe tuple
    /// </summary>
    /// <param name="newTS"></param>
    /// <returns></returns>
    (int, int) ProcessUnsafeTSString(string newTS)
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
    
    #endregion
}
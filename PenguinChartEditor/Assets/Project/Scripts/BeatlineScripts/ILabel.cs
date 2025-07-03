using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public interface ILabel
{
    GameObject LabelObject { get; set; }
    RectTransform LabelRectTransform { get; set; }
    TMP_InputField LabelEntryBox { get; set; }

    string LabelText { get; set; }
    public void HandleManualEndEdit(string newVal);
    public void DeactivateManualInput();
    public void ActivateManualInput();
    public void HandleLabelClick(BaseEventData data);
    public void ConcludeManualEdit();
    public void HandleEntryBoxDeselect();
}

public abstract class Label<DataType> : Event<DataType>, ILabel
{
    [field: SerializeField] public GameObject LabelObject { get; set; }
    [field: SerializeField] public RectTransform LabelRectTransform { get; set; }
    [field: SerializeField] public TMP_InputField LabelEntryBox { get; set; }
    [field: SerializeField] protected TextMeshProUGUI _labelText { get; set; }
    public string LabelText
    {
        get
        {
            return _labelText.text;
        }
        set
        {
            _labelText.text = value;
        }
    }

    void Start()
    {
        if (LabelEntryBox != null) LabelEntryBox.onEndEdit.AddListener(x => HandleManualEndEdit(x));
    }

    public abstract void HandleManualEndEdit(string newVal);
    public void ActivateManualInput()
    {
        LabelEntryBox.gameObject.SetActive(true);
        LabelEntryBox.ActivateInputField();

        LabelEntryBox.text = SongTimelineManager.TempoEvents[Tick].Item1.ToString();
        BeatlinePreviewer.editMode = false;

        SongTimelineManager.DisableChartingInputMap();
    }

    public void DeactivateManualInput()
    {
        LabelEntryBox.gameObject.SetActive(false);
        SongTimelineManager.EnableChartingInputMap();
    }

    public void HandleLabelClick(BaseEventData data)
    {
        var clickdata = (PointerEventData)data;

        CalculateSelectionStatus(clickdata.button);

        // Double click functionality for manual entry of beatline number
        if (!Input.GetKey(KeyCode.LeftControl) && clickdata.button == PointerEventData.InputButton.Left && clickdata.clickCount == 2)
        {
            ActivateManualInput();
        }

        if (DeletePrimed && clickdata.button == PointerEventData.InputButton.Left) DeleteSelection();

        TempoManager.UpdateBeatlines();
    }

    public void ConcludeManualEdit()
    {
        DeactivateManualInput();
        TempoManager.UpdateBeatlines();
    }

    public void HandleEntryBoxDeselect()
    {
        ConcludeManualEdit();
    }

    public int lastTickSelection;
    /// <summary>
    /// Calculate the event(s) to be selected based on the last click event.
    /// </summary>
    /// <param name="clickButton">PointerEventData.button</param>
    /// <param name="targetSelectionSet">The selection hash set that contains this event type's selection data.</param>
    /// <param name="targetEventSet">The keys of a sorted dictionary that holds event data (beatlines, TS, etc)</param>
    public void CalculateSelectionStatus(PointerEventData.InputButton clickButton)
    {
        var selection = GetSelectedEvents();
        List<int> targetEventSet = GetTargetEventSet().Keys.ToList();
        // Goal is to follow standard selection functionality of most productivity programs
        if (clickButton != PointerEventData.InputButton.Left) return;

        // Shift-click functionality
        if (Input.GetKey(KeyCode.LeftShift))
        {
            selection.Clear();

            var minNum = Math.Min(lastTickSelection, Tick);
            var maxNum = Math.Max(lastTickSelection, Tick);
            HashSet<int> selectedEvents = targetEventSet.Where(x => x <= maxNum && x >= minNum).ToHashSet();
            selection.UnionWith(selectedEvents);
        }
        // Left control if item is already selected
        else if (Input.GetKey(KeyCode.LeftControl) && selection.Contains(Tick))
        {
            selection.Remove(Tick);
            return; // prevent lastTickSelection from being stored as an unselected number
        }
        // Left control if item is not currently selected
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            selection.Add(Tick);
        }
        // Regular click, no extra significant keybinds
        else
        {
            selection.Clear();
            selection.Add(Tick);
        }
        // Record the last selection data for shift-click selection
        lastTickSelection = Tick;
    }
}
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public interface ILabel
{
    string LabelText { get; set; }
}

public abstract class Label<T> : Event<T>, ILabel, IPoolable where T : IEventData
{
    #region Components

    [SerializeField] private RectTransform LabelRectTransform;
    [SerializeField] private TMP_InputField LabelEntryBox;
    [SerializeField] private TextMeshProUGUI _labelText;

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

    #endregion

    #region Setup

    protected override bool HasSustainTrail => false;

    private void Start()
    {
        if (LabelEntryBox == null) return;
        
        LabelEntryBox.onEndEdit.AddListener(x => HandleManualEndEdit(x));
        LabelEntryBox.onDeselect.AddListener(x => HandleEntryBoxDeselect());
    }

    #endregion

    #region Manual Input / Entry Box Handling

    protected abstract T ProcessUnsafeLabelString(string newVal);

    private void ActivateManualInput()
    {
        if (LabelEntryBox == null) return;
        LabelEntryBox.gameObject.SetActive(true);

        if (!Visible || !LaneData.ContainsKey(Tick)) return;
        editTick = Tick;

        LabelEntryBox.gameObject.SetActive(true);
        LabelEntryBox.ActivateInputField();

        LabelEntryBox.text = representedData.ToString();
        Chart.showPreviewers = false;
        SongTime.DisableChartingInputMap();
    }

    /// <summary>
    /// This prevents label entry boxes from appearing on unrequested labels.
    /// When initializing a label, this tick is set to the current tick of the label,
    /// and when refreshing labels, if the ticks of the labels do not match, then the entry box should be hidden.
    /// </summary>
    private static int editTick = -1;

    private void HandleManualEndEdit(string newVal)
    {
        LaneData[Tick] = ProcessUnsafeLabelString(newVal);

        if (typeof(T) == typeof(BPMData)) Chart.SyncTrackInstrument.RecalculateTempoEventDictionary(Tick);

        ConcludeManualEdit();
        Chart.SyncTrackInPlaceRefresh();
    }

    private void HandleEntryBoxDeselect()
    {
        ConcludeManualEdit();
    }

    private void ConcludeManualEdit()
    {
        Chart.showPreviewers = true;
        DeactivateManualInput();
    }

    private void DeactivateManualInput()
    {
        LabelEntryBox.gameObject.SetActive(false);

        Chart.showPreviewers = true;
        SongTime.EnableChartingInputMap();
    }

    #endregion

    #region Init/Deinit Label

    protected override void InitializeEvent()
    {
        LabelText = representedData.ToString();
        
        InitializeLabel();
        
        if (editTick != Tick) DeactivateManualInput();
    }

    protected abstract void InitializeLabel();

    protected override void InitializeEventAsPreviewer()
    {
        LabelText = representedData.ToString();
    }

    protected override void UpdatePosition()
    {
        transform.localPosition = 
            new Vector2(
                transform.localPosition.x, 
                GetDefaultZ() - (LabelRectTransform.rect.height / 2)
                );
    }

    public virtual void SetLabelInactive()
    {
        Visible = false;
        DeactivateManualInput();
    }

    #endregion

    #region Double Click Implementation (Activate Labels)

    private int clickCount = 0;
    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);

        if (pointerEventData.button != PointerEventData.InputButton.Left || !LaneData.ContainsKey(Tick)) return;
        
        clickCount++;

        // Double click functionality for manual entry of beatline number
        // eventData.clickCount does not work here - pointerDown and pointerUp do not trigger click count for some reason
        // so manual coroutine solution is here to circumvent that issue
        if (!Input.GetKey(KeyCode.LeftControl) &&
            Chart.IsModificationAllowed() && 
            clickCount >= 2)
        {
            ActivateManualInput();
            Chart.InPlaceRefresh();

            // if you click too fast, clickCount will exceed 2
            // at some point and will never be able to reset
            // reset here to avoid arbitrarily bricked label object
            clickCount = 0;
        }

        if (clickCount == 1 && gameObject.activeInHierarchy) StartCoroutine(TriggerDoubleClick());
    }

    private IEnumerator TriggerDoubleClick()
    {
        yield return clickCooldown;
        clickCount = 0;
    }

    #endregion
}
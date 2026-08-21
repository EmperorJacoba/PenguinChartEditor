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

    [SerializeField] private Canvas labelCanvas;
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

        labelCanvas.worldCamera = CameraHighwayScaler.instance.orthographicSceneCamera;
    }

    #endregion

    #region Manual Input / Entry Box Handling

    protected abstract T ProcessUnsafeLabelString(string newVal);

    private void ActivateManualInput()
    {
        if (LabelEntryBox == null) return;

        if (!Visible || !LaneData.ContainsKey(Tick)) return;
        editTick = Tick;
        
        _labelText.gameObject.SetActive(false);
        LabelEntryBox.gameObject.SetActive(true);
        LabelEntryBox.ActivateInputField();

        LabelEntryBox.text = representedData.ToString();
        
        Chart.showPreviewers = false;
        SongTime.StopPlaybackAndTimeEditActions();
        
        Chart.InPlaceRefresh();
    }

    /// <summary>
    /// This prevents label entry boxes from appearing on unrequested labels.
    /// When initializing a label, this tick is set to the current tick of the label,
    /// and when refreshing labels, if the ticks of the labels do not match, then the entry box should be hidden.
    /// </summary>
    private static int editTick = -1;

    private void HandleManualEndEdit(string newVal)
    {
        Chart.SyncTrackInstrument.CreateEvent(Tick, Lane, ProcessUnsafeLabelString(newVal));
        
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
        _labelText.gameObject.SetActive(true);
        
        Chart.showPreviewers = true;
        SongTime.AllowPlaybackAndTimeEditActions();
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
            new Vector3(
                transform.localPosition.x, 
                transform.localPosition.y,
                GetDefaultZ()
                );
    }

    public virtual void SetLabelInactive()
    {
        Visible = false;
        DeactivateManualInput();
    }

    #endregion

    #region Double Click Implementation (Activate Labels)

    protected override bool HasDoubleClickAction() => true;
    protected override void ExecuteDoubleClickAction() => ActivateManualInput();

    #endregion
}
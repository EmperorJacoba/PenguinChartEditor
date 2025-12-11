using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

public interface ILabel
{
    bool Visible { get; set; }
    string LabelText { get; set; }
    bool Selected { get; set; }
    string ConvertDataToPreviewString();
    bool CheckForSelection();
    void DeactivateManualInput();
}

public abstract class Label<T> : Event<T>, ILabel where T : IEventData
{
    [field: SerializeField] public GameObject LabelObject { get; set; }
    [field: SerializeField] public RectTransform LabelRectTransform { get; set; }
    [field: SerializeField] public TMP_InputField LabelEntryBox { get; set; }
    [field: SerializeField] protected TextMeshProUGUI _labelText { get; set; }

    public abstract string ConvertDataToPreviewString();
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
        if (LabelEntryBox != null)
        {
            LabelEntryBox.onEndEdit.AddListener(x => HandleManualEndEdit(x));
            LabelEntryBox.onDeselect.AddListener(x => HandleEntryBoxDeselect());
        }
    }

    public override int Tick
    {
        get
        {
            return _tick;
        }
    }
    protected int _tick;

    public abstract void HandleManualEndEdit(string newVal);

    /// <summary>
    /// This prevents label entry boxes from appearing on unrequested labels.
    /// When initializing a label, this tick is set to the current tick of the label,
    /// and when refreshing labels, if the ticks of the labels do not match, then the entry box should be hidden.
    /// </summary>
    protected static int editTick = -1;

    public void ActivateManualInput()
    {
        if (LabelEntryBox == null) return;
        LabelEntryBox.gameObject.SetActive(true);
        
        if (!LabelObject.activeInHierarchy || !LaneData.ContainsKey(Tick)) return;
        editTick = Tick;

        LabelEntryBox.gameObject.SetActive(true);
        LabelEntryBox.ActivateInputField();

        LabelEntryBox.text = ConvertDataToPreviewString();
        EventPreviewer.Hide();

        SongTime.DisableChartingInputMap();
    }

    public void DeactivateManualInput()
    {
        LabelEntryBox.gameObject.SetActive(false);

        EventPreviewer.Show();
        SongTime.EnableChartingInputMap();
    }

    public void ConcludeManualEdit()
    {
        Chart.editMode = true;
        DeactivateManualInput();
        Chart.Refresh();
    }

    public void HandleEntryBoxDeselect()
    {
        ConcludeManualEdit();
    }

    public virtual void InitializeLabel(int tick)
    {
        _tick = tick;
        Visible = true;
        UpdatePosition(Waveform.GetWaveformRatio(_tick), Chart.instance.SceneDetails.HighwayLength);

        LabelText = ConvertDataToPreviewString();
        Selected = CheckForSelection();

        if (editTick != _tick) DeactivateManualInput();
    }

    public virtual void SetLabelInactive()
    {
        Visible = false;
        DeactivateManualInput();
    }

    int clickCount = 0;
    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);

        if (pointerEventData.button != PointerEventData.InputButton.Left) return;
        
        clickCount++;

        // Double click functionality for manual entry of beatline number
        // eventData.clickCount does not work here - pointerDown and pointerUp do not trigger click count for some reason
        // so manual coroutine solution is here to circumvent that issue
        if (!Input.GetKey(KeyCode.LeftControl) &&
            Chart.IsEditAllowed() && 
            clickCount == 2)
        {
            ActivateManualInput();
            RefreshLane();

            // if you click too fast, clickCount will exceed 2
            // at some point and will never be able to reset
            // reset here to avoid arbitrarily bricked label object
            clickCount = 0;
        }

        if (clickCount == 1 && gameObject.activeInHierarchy) StartCoroutine(TriggerDoubleClick());
    }

    private static WaitForSeconds clickCooldown = new(0.15f);
    IEnumerator TriggerDoubleClick()
    {
        yield return clickCooldown;
        clickCount = 0;
    }

    public void UpdatePosition(double percentOfScreen, float screenHeight)
    {
        var yScreenProportion = (float)percentOfScreen * screenHeight;
        transform.localPosition = new Vector3(transform.localPosition.x, yScreenProportion - (LabelRectTransform.rect.height / 2));
    }
}
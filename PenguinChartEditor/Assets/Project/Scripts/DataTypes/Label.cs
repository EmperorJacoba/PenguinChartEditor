using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public interface ILabel
{
    /// <summary>
    /// Reference to the game object label itself.
    /// </summary>
    GameObject LabelObject { get; set; }
    RectTransform LabelRectTransform { get; set; }
    TMP_InputField LabelEntryBox { get; set; }
    string LabelText { get; set; }
    public void HandleManualEndEdit(string newVal);
    public void DeactivateManualInput();
    public void ActivateManualInput();
    public void ConcludeManualEdit();
    public void HandleEntryBoxDeselect();
    public string ConvertDataToPreviewString();
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
            LabelEntryBox.onEndEdit.AddListener(x => HandleManualEndEdit(x));
    }

    public abstract void HandleManualEndEdit(string newVal);
    public void ActivateManualInput()
    {
        if (LabelObject.activeInHierarchy != true || !GetEventData().Events.ContainsKey(Tick)) return;

        LabelEntryBox.gameObject.SetActive(true);
        LabelEntryBox.ActivateInputField();

        LabelEntryBox.text = ConvertDataToPreviewString();
        BeatlinePreviewer.editMode = false;

        SongTimelineManager.DisableChartingInputMap();
        
    }

    public void DeactivateManualInput()
    {
        LabelEntryBox.gameObject.SetActive(false);
        SongTimelineManager.EnableChartingInputMap();
    }

    public void ConcludeManualEdit()
    {
        BeatlinePreviewer.editMode = true;
        DeactivateManualInput();
        TempoManager.UpdateBeatlines();
    }

    public void HandleEntryBoxDeselect()
    {
        ConcludeManualEdit();
    }

    int clickCount = 0;
    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);
        //if (Visible != true) return;

        clickCount++;
        // Double click functionality for manual entry of beatline number
        // eventData.clickCount does not work here - pointerDown and pointerUp do not trigger click count for some reason
        // so manual coroutine solution is here to circumvent that issue
        if (!Input.GetKey(KeyCode.LeftControl) && pointerEventData.button == PointerEventData.InputButton.Left && clickCount == 2)
        {
            ActivateManualInput();
            TempoManager.UpdateBeatlines();
        }

        if (clickCount == 1) StartCoroutine(TriggerDoubleClick());
    }

    private static WaitForSeconds clickCooldown = new(0.5f);
    IEnumerator TriggerDoubleClick()
    {
        yield return clickCooldown;
        clickCount = 0;
    }
}
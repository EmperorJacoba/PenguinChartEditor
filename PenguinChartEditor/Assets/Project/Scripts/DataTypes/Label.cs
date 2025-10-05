using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
            LabelEntryBox.onEndEdit.AddListener(x => HandleManualEndEdit(x));
    }

    public abstract void HandleManualEndEdit(string newVal);
    public void ActivateManualInput()
    {
        if (!LabelObject.activeInHierarchy || !GetEventData().Events.ContainsKey(Tick)) return;

        try { LabelEntryBox.gameObject.SetActive(true); } catch { return; } // omg unity shut up about this (genuinely this should not be possible)

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
        Chart.Refresh();
    }

    public void HandleEntryBoxDeselect()
    {
        ConcludeManualEdit();
    }

    public void SetLabelActive()
    {
        Visible = true;
        LabelText = ConvertDataToPreviewString();
        Selected = CheckForSelection();
    }

    public void SetLabelInactive()
    {
        Visible = false;
        DeactivateManualInput();
    }

    int clickCount = 0;
    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);

        clickCount++;
        // Double click functionality for manual entry of beatline number
        // eventData.clickCount does not work here - pointerDown and pointerUp do not trigger click count for some reason
        // so manual coroutine solution is here to circumvent that issue
        if (!Input.GetKey(KeyCode.LeftControl) && pointerEventData.button == PointerEventData.InputButton.Left && clickCount == 2)
        {
            ActivateManualInput();
            Chart.Refresh();
        }

        if (clickCount == 1 && gameObject.activeInHierarchy) StartCoroutine(TriggerDoubleClick());
    }

    private static WaitForSeconds clickCooldown = new(0.5f);
    IEnumerator TriggerDoubleClick()
    {
        yield return clickCooldown;
        clickCount = 0;
    }
}
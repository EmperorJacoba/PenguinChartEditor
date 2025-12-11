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
        if (LabelEntryBox == null) return;
        LabelEntryBox.gameObject.SetActive(true);
        
        if (!LabelObject.activeInHierarchy || !LaneData.ContainsKey(Tick)) return;

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

    public virtual void InitializeLabel()
    {
        LabelText = ConvertDataToPreviewString();
        Selected = CheckForSelection();
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

        if (pointerEventData.button == PointerEventData.InputButton.Left) clickCount++;

        // Double click functionality for manual entry of beatline number
        // eventData.clickCount does not work here - pointerDown and pointerUp do not trigger click count for some reason
        // so manual coroutine solution is here to circumvent that issue
        if (!Input.GetKey(KeyCode.LeftControl) &&
            Chart.IsEditAllowed() && 
            pointerEventData.button == PointerEventData.InputButton.Left && 
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

    private static WaitForSeconds clickCooldown = new(0.5f);
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
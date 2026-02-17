using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class Section : Event<SectionData>
{
    protected override bool HasSustainTrail => false;

    private void Start()
    {
        if (IsPreviewEvent) return;
        sectionNameModifierInputField.onEndEdit.AddListener(HandleManualEndEdit);
    }
    
    private void HandleManualEndEdit(string newSectionName)
    {
        LaneData[Tick] = new SectionData(newSectionName);

        Chart.showPreviewers = true;
        DeactivateManualInput();
        Chart.InPlaceRefresh();
    }

    public override int Lane
    {
        get => 0;
        set {} // not needed
    }
    public override SelectionSet<SectionData> Selection => Chart.SectionInstrument.GetLaneSelection();
    protected override LaneSet<SectionData> LaneData => Chart.SectionInstrument.GetLaneData();
    public override IInstrument ParentInstrument => Chart.SectionInstrument;

    [SerializeField] private TMP_Text displayedSectionName;
    [FormerlySerializedAs("sectionNameModifier")] [SerializeField] private TMP_InputField sectionNameModifierInputField;

    protected override void InitializeEvent()
    {
        if (Chart.LoadedInstrument != ParentInstrument) Visible = false;
        
        displayedSectionName.text = representedData.Name;
        
        if (editTick != Tick) DeactivateManualInput();
    }

    protected override void InitializeEventAsPreviewer() => InitializeEvent();
    
    protected override void UpdatePosition()
    {
        transform.position =
            new Vector3(
                Camera.main.transform.position.x,
                transform.position.y,
                GetGuaranteedNegativeZ()
            );
    }

    protected override bool HasDoubleClickAction() => true;
    protected override void ExecuteDoubleClickAction() => ActivateManualInput();

    private static int editTick = -1;
    private void ActivateManualInput()
    {
        if (sectionNameModifierInputField == null) return;

        if (!Visible || !LaneData.ContainsKey(Tick)) return;
        
        editTick = Tick;
        displayedSectionName.gameObject.SetActive(false);
        sectionNameModifierInputField.gameObject.SetActive(true);
        sectionNameModifierInputField.ActivateInputField();
        sectionNameModifierInputField.text = representedData.Name;

        Chart.showPreviewers = false;
        SongTime.DisableChartingInputMap();
        
        Chart.InPlaceRefresh();
    }

    private void DeactivateManualInput()
    {
        if (sectionNameModifierInputField == null) return;
        
        sectionNameModifierInputField.gameObject.SetActive(false);
        displayedSectionName.gameObject.SetActive(true);
        
        Chart.showPreviewers = true;
        SongTime.EnableChartingInputMap();
    }
}
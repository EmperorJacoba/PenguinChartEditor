using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class FiveFretNoteKeybindManager : MonoBehaviour
{
    private InputMap inputMap;
    [SerializeField] private TMP_Dropdown modifierDropdown;
    [SerializeField] private ExtendedSustainController esc;
    [FormerlySerializedAs("sustainCustomInput")] [SerializeField] private CustomSustainInputter sustainCustomInputPlacement;
    [SerializeField] private CustomSustainInputter sustainCustomInputSelection;
    private void Awake()
    {
        inputMap = new InputMap();
        inputMap.Enable();

        inputMap.ExternalCharting.SwitchNotePlacementMode.performed += x =>
        {
            FiveFretNotePreviewer.openNoteEditing = !FiveFretNotePreviewer.openNoteEditing;
            UpdatePreviewer?.Invoke();
        };

        inputMap.Charting.ForceTap.performed += x => ChangeModifier(FiveFretNotePreviewer.NoteOption.tap);
        inputMap.Charting.ForceStrum.performed += x => ChangeModifier(FiveFretNotePreviewer.NoteOption.strum);
        inputMap.Charting.ForceHopo.performed += x => ChangeModifier(FiveFretNotePreviewer.NoteOption.hopo);
        inputMap.Charting.ForceDefault.performed += x => ChangeModifier(FiveFretNotePreviewer.NoteOption.natural);

        inputMap.Charting.SustainMax.performed += x => SetCurrentSustain(SongTime.SongLengthTicks);
        inputMap.Charting.SustainZero.performed += x => SetCurrentSustain(0);
        inputMap.Charting.SustainCustom.performed += x =>
        {
            if (Chart.GetActiveInstrument<FiveFretInstrument>().IsNoteSelectionEmpty())
            {
                sustainCustomInputPlacement.ActivateCustomInput();
            }
            else
            {
                sustainCustomInputSelection.ActivateCustomInput();
            }
        };

        inputMap.Charting.SustainExtended.performed += x => esc.SetExtendedSustains(!UserSettings.ExtSustains);

        inputMap.Charting.SetLane0.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.open);
        inputMap.Charting.SetLane1.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.green);
        inputMap.Charting.SetLane2.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.red);
        inputMap.Charting.SetLane3.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.yellow);
        inputMap.Charting.SetLane4.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.blue);
        inputMap.Charting.SetLane5.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.orange);

        inputMap.Charting.SetEqualSpacing.performed += x => Chart.GetActiveInstrument<FiveFretInstrument>().SetEqualSpacing();
    }

    public delegate void UpdatePreviewerDelegate();
    public static event UpdatePreviewerDelegate UpdatePreviewer;

    public void ChangeModifier(FiveFretNotePreviewer.NoteOption newMode)
    {
        var instrument = Chart.GetActiveInstrument<FiveFretInstrument>();
        if (!instrument.IsNoteSelectionEmpty())
        {
            if (newMode == FiveFretNotePreviewer.NoteOption.natural)
            {
                instrument.NaturalizeSelection();
                return;
            }
            instrument.SetSelectionToFlag(MatchNoteModeToFlagType(newMode));
            return;
        }

        if (newMode == FiveFretNotePreviewer.currentPlacementMode) return;

        modifierDropdown.value = (int)newMode;
        FiveFretNotePreviewer.currentPlacementMode = newMode;
        UpdatePreviewer?.Invoke();
    }

    public FiveFretNoteData.FlagType MatchNoteModeToFlagType(FiveFretNotePreviewer.NoteOption mode)
    {
        return mode switch
        {
            FiveFretNotePreviewer.NoteOption.tap => FiveFretNoteData.FlagType.tap,
            FiveFretNotePreviewer.NoteOption.strum => FiveFretNoteData.FlagType.strum,
            FiveFretNotePreviewer.NoteOption.hopo => FiveFretNoteData.FlagType.hopo,
            _ => throw new System.Exception("Invalid mode <=> flag match")
        };
    }

    public void SetCurrentSustain(int ticks)
    {
        var instrument = Chart.GetActiveInstrument<FiveFretInstrument>();
        if (!instrument.IsNoteSelectionEmpty())
        {
            instrument.SetSelectionSustain(ticks);
            return;
        }

        Previewer.SetDefaultSustainLength(true, ticks);
        sustainCustomInputPlacement.ClearInput();
        UpdatePreviewer?.Invoke();
    }

    public void SetSelectionLane(FiveFretInstrument.LaneOrientation lane)
    {
        var instrument = Chart.GetActiveInstrument<FiveFretInstrument>();

        instrument.SetSelectionToNewLane(lane);
    }
}

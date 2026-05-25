using System;
using UnityEngine;
using UnityEngine.Serialization;

public class FiveFretNoteKeybindManager : MonoBehaviour
{
    private InputMap inputMap;
    [SerializeField] private FiveFretFlagController flagPlacementController;
    [SerializeField] private ExtendedSustainController esc;
    [FormerlySerializedAs("sustainCustomInput")] [SerializeField] private CustomSustainInputter sustainCustomInputPlacement;
    [SerializeField] private CustomSustainInputter sustainCustomInputSelection;
    
    private void Awake()
    {
        inputMap = new InputMap();
        inputMap.Enable();

        inputMap.FiveFretCharting.SwitchOpenAndFrettedChartingMode.performed += x =>
        {
            FiveFretNotePreviewer.openNoteEditing = !FiveFretNotePreviewer.openNoteEditing;
            UpdatePreviewer?.Invoke();
        };
        
        inputMap.FiveFretCharting.ForceTap.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.tap);
        
        inputMap.FiveFretCharting.ForceStrum.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.strum);
        
        inputMap.FiveFretCharting.ForceHopo.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.hopo);
        
        inputMap.FiveFretCharting.ForceDefault.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.natural);

        inputMap.SustainCommands.SustainMax.performed += x => SetCurrentSustain(SongTime.SongLengthTicks);
        inputMap.SustainCommands.SustainZero.performed += x => SetCurrentSustain(0);
        inputMap.SustainCommands.SustainCustom.performed += x =>
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

        inputMap.SustainCommands.SustainExtended.performed += x => esc.SetExtendedSustains(!Chart.settings.ExtendedSustains);

        inputMap.FiveFretCharting.SetLaneOpen.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.open);
        inputMap.FiveFretCharting.SetLaneGreen.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.green);
        inputMap.FiveFretCharting.SetLaneRed.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.red);
        inputMap.FiveFretCharting.SetLaneYellow.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.yellow);
        inputMap.FiveFretCharting.SetLaneBlue.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.blue);
        inputMap.FiveFretCharting.SetLaneOrange.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.orange);

        inputMap.PenguinChartingUIShortcuts.SetEqualSpacing.performed += x => Chart.GetActiveInstrument<FiveFretInstrument>().SetEqualSpacing();
    }

    private void OnDestroy()
    {
        inputMap.Disable();
    }

    public delegate void UpdatePreviewerDelegate();
    public static event UpdatePreviewerDelegate UpdatePreviewer;
    public static void InvokeUpdatePreviewer() => UpdatePreviewer?.Invoke();

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
        Chart.LoadedInstrument.SetSelectionToNewLane((int)lane);
    }
}

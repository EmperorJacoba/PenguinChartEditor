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

        inputMap.FiveFret.SwitchOpenAndFrettedChartingMode.performed += x =>
        {
            FiveFretNotePreviewer.openNoteEditing = !FiveFretNotePreviewer.openNoteEditing;
            UpdatePreviewer?.Invoke();
        };
        
        inputMap.FiveFret.ForceTap.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.tap);
        
        inputMap.FiveFret.ForceStrum.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.strum);
        
        inputMap.FiveFret.ForceHopo.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.hopo);
        
        inputMap.FiveFret.ForceDefault.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.natural);

        inputMap.Sustains.SustainMax.performed += x => SetCurrentSustain(SongTime.SongLengthTicks);
        inputMap.Sustains.SustainZero.performed += x => SetCurrentSustain(0);
        inputMap.Sustains.SustainCustom.performed += x =>
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

        inputMap.Sustains.SustainExtended.performed += x => esc.SetExtendedSustains(!Chart.settings.ExtendedSustains);

        inputMap.FiveFret.SetLaneOpen.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.open);
        inputMap.FiveFret.SetLaneGreen.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.green);
        inputMap.FiveFret.SetLaneRed.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.red);
        inputMap.FiveFret.SetLaneYellow.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.yellow);
        inputMap.FiveFret.SetLaneBlue.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.blue);
        inputMap.FiveFret.SetLaneOrange.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.orange);

        inputMap.UIShortcuts.SetEqualSpacing.performed += x => Chart.GetActiveInstrument<FiveFretInstrument>().SetEqualSpacing();
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

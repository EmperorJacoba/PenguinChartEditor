using System;
using UnityEngine;
using UnityEngine.Serialization;

public class FiveFretNoteKeybindManager : MonoBehaviour
{
    [SerializeField] private FiveFretFlagController flagPlacementController;
    [SerializeField] private ExtendedSustainController esc;
    [FormerlySerializedAs("sustainCustomInput")] [SerializeField] private CustomSustainInputter sustainCustomInputPlacement;
    [SerializeField] private CustomSustainInputter sustainCustomInputSelection;
    
    private void Awake()
    {
        Chart.inputMap.FiveFret.SwitchOpenAndFrettedChartingMode.performed += x =>
        {
            FiveFretNotePreviewer.openNoteEditing = !FiveFretNotePreviewer.openNoteEditing;
            UpdatePreviewer?.Invoke();
        };
        
        Chart.inputMap.FiveFret.ForceTap.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.tap);
        
        Chart.inputMap.FiveFret.ForceStrum.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.strum);
        
        Chart.inputMap.FiveFret.ForceHopo.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.hopo);
        
        Chart.inputMap.FiveFret.ForceDefault.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.natural);

        Chart.inputMap.Sustains.SustainMax.performed += x => SetCurrentSustain(SongTime.SongLengthTicks);
        Chart.inputMap.Sustains.SustainZero.performed += x => SetCurrentSustain(0);
        Chart.inputMap.Sustains.SustainCustom.performed += x =>
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

        Chart.inputMap.Sustains.SustainExtended.performed += x => esc.SetExtendedSustains(!Chart.settings.ExtendedSustains);

        Chart.inputMap.FiveFret.SetLaneOpen.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.open);
        Chart.inputMap.FiveFret.SetLaneGreen.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.green);
        Chart.inputMap.FiveFret.SetLaneRed.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.red);
        Chart.inputMap.FiveFret.SetLaneYellow.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.yellow);
        Chart.inputMap.FiveFret.SetLaneBlue.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.blue);
        Chart.inputMap.FiveFret.SetLaneOrange.performed += x => SetSelectionLane(FiveFretInstrument.LaneOrientation.orange);

        Chart.inputMap.UIShortcuts.SetEqualSpacing.performed += x => Chart.GetActiveInstrument<FiveFretInstrument>().SetEqualSpacing();
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

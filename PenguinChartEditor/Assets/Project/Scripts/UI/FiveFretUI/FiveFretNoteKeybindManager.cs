using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class FiveFretNoteKeybindManager : MonoBehaviour
{
    [SerializeField] private FiveFretFlagController flagPlacementController;
    [SerializeField] private ExtendedSustainController esc;
    [FormerlySerializedAs("sustainCustomInput")] [SerializeField] private CustomSustainInputter sustainCustomInputPlacement;
    [SerializeField] private CustomSustainInputter sustainCustomInputSelection;
    
    private void Awake()
    {
        Chart.inputMap.FiveFret.SwitchOpenAndFrettedChartingMode.performed += ToggleChartingMode;
        Chart.inputMap.FiveFret.ForceTap.performed += ChangeModifierTap;
        Chart.inputMap.FiveFret.ForceStrum.performed += ChangeModifierStrum;
        Chart.inputMap.FiveFret.ForceHopo.performed += ChangeModifierHopo;
        Chart.inputMap.FiveFret.ForceDefault.performed += ChangeModifierDefault;

        Chart.inputMap.Sustains.SustainMax.performed += SetSustainMax;
        Chart.inputMap.Sustains.SustainZero.performed += SetSustainZero;
        Chart.inputMap.Sustains.SustainCustom.performed += SetSustainCustom;

        Chart.inputMap.Sustains.SustainExtended.performed += SetSustainExtended;
        Chart.inputMap.FiveFret.SetLaneOpen.performed += SetLaneP;
        Chart.inputMap.FiveFret.SetLaneGreen.performed += SetLaneG;
        Chart.inputMap.FiveFret.SetLaneRed.performed += SetLaneR;
        Chart.inputMap.FiveFret.SetLaneYellow.performed += SetLaneY;
        Chart.inputMap.FiveFret.SetLaneBlue.performed += SetLaneB;
        Chart.inputMap.FiveFret.SetLaneOrange.performed += SetLaneO;
        
        Chart.inputMap.UIShortcuts.SetEqualSpacing.performed += SetEqualSpacing;
    }

    private void OnDestroy()
    {
        Chart.inputMap.FiveFret.SwitchOpenAndFrettedChartingMode.performed -= ToggleChartingMode;
        Chart.inputMap.FiveFret.ForceTap.performed -= ChangeModifierTap;
        Chart.inputMap.FiveFret.ForceStrum.performed -= ChangeModifierStrum;
        Chart.inputMap.FiveFret.ForceHopo.performed -= ChangeModifierHopo;
        Chart.inputMap.FiveFret.ForceDefault.performed -= ChangeModifierDefault;

        Chart.inputMap.Sustains.SustainMax.performed -= SetSustainMax;
        Chart.inputMap.Sustains.SustainZero.performed -= SetSustainZero;
        Chart.inputMap.Sustains.SustainCustom.performed -= SetSustainCustom;

        Chart.inputMap.Sustains.SustainExtended.performed -= SetSustainExtended;
        Chart.inputMap.FiveFret.SetLaneOpen.performed -= SetLaneP;
        Chart.inputMap.FiveFret.SetLaneGreen.performed -= SetLaneG;
        Chart.inputMap.FiveFret.SetLaneRed.performed -= SetLaneR;
        Chart.inputMap.FiveFret.SetLaneYellow.performed -= SetLaneY;
        Chart.inputMap.FiveFret.SetLaneBlue.performed -= SetLaneB;
        Chart.inputMap.FiveFret.SetLaneOrange.performed -= SetLaneO;
        
        Chart.inputMap.UIShortcuts.SetEqualSpacing.performed -= SetEqualSpacing;
    }

    public delegate void UpdatePreviewerDelegate();
    public static event UpdatePreviewerDelegate UpdatePreviewer;
    public static void InvokeUpdatePreviewer() => UpdatePreviewer?.Invoke();

    private void SetSustainZero(InputAction.CallbackContext _) => SetCurrentSustain(0);
    private void SetSustainMax(InputAction.CallbackContext _) => SetCurrentSustain(SongTime.SongLengthTicks);
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
    
    private void SetSustainCustom(InputAction.CallbackContext _)
    {
        if (Chart.GetActiveInstrument<FiveFretInstrument>().IsNoteSelectionEmpty())
        {
            sustainCustomInputPlacement.ActivateCustomInput();
        }
        else
        {
            sustainCustomInputSelection.ActivateCustomInput();
        }
    }

    private void SetSustainExtended(InputAction.CallbackContext _) =>
        esc.SetExtendedSustains(!Chart.settings.ExtendedSustains);

    public void SetSelectionLane(FiveFretInstrument.LaneOrientation lane)
    {
        Chart.LoadedInstrument.SetSelectionToNewLane((int)lane);
    }
    
    private void ChangeModifierTap(InputAction.CallbackContext _) => 
        flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.tap);
    private void ChangeModifierStrum(InputAction.CallbackContext _) => 
        flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.strum);
    private void ChangeModifierHopo(InputAction.CallbackContext _) => 
        flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.hopo);
    private void ChangeModifierDefault(InputAction.CallbackContext _) => 
        flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.natural);

    private void ToggleChartingMode(InputAction.CallbackContext _)
    {
        FiveFretNotePreviewer.openNoteEditing = !FiveFretNotePreviewer.openNoteEditing;
        UpdatePreviewer?.Invoke();
    }

    private void SetLaneP(InputAction.CallbackContext _) => SetSelectionLane(FiveFretInstrument.LaneOrientation.open);
    private void SetLaneG(InputAction.CallbackContext _) => SetSelectionLane(FiveFretInstrument.LaneOrientation.green);
    private void SetLaneR(InputAction.CallbackContext _) => SetSelectionLane(FiveFretInstrument.LaneOrientation.red);
    private void SetLaneY(InputAction.CallbackContext _) => SetSelectionLane(FiveFretInstrument.LaneOrientation.yellow);
    private void SetLaneB(InputAction.CallbackContext _) => SetSelectionLane(FiveFretInstrument.LaneOrientation.blue);
    private void SetLaneO(InputAction.CallbackContext _) => SetSelectionLane(FiveFretInstrument.LaneOrientation.orange);
    
    private void SetEqualSpacing(InputAction.CallbackContext _) => Chart.GetActiveInstrument<FiveFretInstrument>().SetEqualSpacing();
    
}

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

        inputMap.ExternalCharting.SwitchNotePlacementMode.performed += x =>
        {
            FiveFretNotePreviewer.openNoteEditing = !FiveFretNotePreviewer.openNoteEditing;
            UpdatePreviewer?.Invoke();
        };
        
        inputMap.Charting.ForceTap.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.tap);
        
        inputMap.Charting.ForceStrum.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.strum);
        
        inputMap.Charting.ForceHopo.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.hopo);
        
        inputMap.Charting.ForceDefault.performed += x => 
            flagPlacementController.ChangeModifierExternal(FiveFretNotePreviewer.NoteOption.natural);

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

    private void OnDestroy()
    {
        inputMap.Dispose();
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

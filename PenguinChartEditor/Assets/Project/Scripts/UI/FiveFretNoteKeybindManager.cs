using TMPro;
using UnityEngine;

public class FiveFretNoteKeybindManager : MonoBehaviour
{
    InputMap inputMap;
    [SerializeField] TMP_Dropdown modifierDropdown;
    [SerializeField] ExtendedSustainController esc;
    [SerializeField] FiveFretSustainController ffsc;
    private void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.ExternalCharting.SwitchNotePlacementMode.performed += x =>
        {
            FiveFretNotePreviewer.openNoteEditing = !FiveFretNotePreviewer.openNoteEditing;
            UpdatePreviewer?.Invoke();
        };
        inputMap.Charting.ForceTap.performed += x => ChangeModifier(FiveFretNotePreviewer.NoteOption.tap);
        inputMap.Charting.ForceStrum.performed += x => ChangeModifier(FiveFretNotePreviewer.NoteOption.strum);
        inputMap.Charting.ForceHopo.performed += x => ChangeModifier(FiveFretNotePreviewer.NoteOption.hopo);
        inputMap.Charting.ForceDefault.performed += x => ChangeModifier(FiveFretNotePreviewer.NoteOption.dynamic);

        inputMap.Charting.SustainMax.performed += x => SetPreviewerSustain(SongTime.SongLengthTicks);
        inputMap.Charting.SustainZero.performed += x => SetPreviewerSustain(0);
        inputMap.Charting.SustainExtended.performed += x => esc.SetExtendedSustains(!UserSettings.ExtSustains);
        inputMap.Charting.SustainCustom.performed += x => ffsc.ActivateCustomInput();
    }

    public delegate void UpdatePreviewerDelegate();
    public static event UpdatePreviewerDelegate UpdatePreviewer;

    public void ChangeModifier(FiveFretNotePreviewer.NoteOption newMode)
    {
        if (newMode == FiveFretNotePreviewer.currentPlacementMode) return;
        modifierDropdown.value = (int)newMode;
        FiveFretNotePreviewer.currentPlacementMode = newMode;
        UpdatePreviewer?.Invoke();
    }

    public void SetPreviewerSustain(int ticks)
    {
        FiveFretNotePreviewer.defaultSustain = ticks;
        ffsc.ClearInput();
        UpdatePreviewer?.Invoke();
    }
}

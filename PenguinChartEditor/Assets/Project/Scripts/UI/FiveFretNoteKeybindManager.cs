using TMPro;
using UnityEngine;

public class FiveFretNoteKeybindManager : MonoBehaviour
{
    InputMap inputMap;
    [SerializeField] TMP_Dropdown modifierDropdown;
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
}

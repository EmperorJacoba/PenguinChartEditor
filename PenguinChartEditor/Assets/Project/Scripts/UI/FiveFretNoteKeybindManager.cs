using UnityEngine;

public class FiveFretNoteKeybindManager : MonoBehaviour
{
    InputMap inputMap;
    private void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.ExternalCharting.SwitchNotePlacementMode.performed += x =>
        {
            FiveFretNotePreviewer.openNoteEditing = !FiveFretNotePreviewer.openNoteEditing;
            UpdatePreviewer?.Invoke();
        };
    }

    public delegate void UpdatePreviewerDelegate();
    public static event UpdatePreviewerDelegate UpdatePreviewer;
}

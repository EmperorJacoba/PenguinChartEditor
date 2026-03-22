using System;
using TMPro;
using UnityEngine;

public class FiveFretFlagController : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown.onValueChanged.AddListener(ChangeModifier);
    }

    public void ChangeModifierExternal(FiveFretNotePreviewer.NoteOption option) => dropdown.value = (int)option;
    private void ChangeModifier(int option)
    {
        var newMode = (FiveFretNotePreviewer.NoteOption)option;
        if (Input.GetKey(KeyCode.LeftControl)) return;
        
        var instrument = Chart.GetActiveInstrument<FiveFretInstrument>();
        if (!instrument.IsNoteSelectionEmpty())
        {
            if (newMode == FiveFretNotePreviewer.NoteOption.natural)
            {
                instrument.NaturalizeSelection();
                return;
            }
            instrument.SetSelectionToFlag(InstrumentMetadata.MatchNoteModeToFlagType(newMode));
            return;
        }

        if (newMode == FiveFretNotePreviewer.currentPlacementMode) return;
        
        FiveFretNotePreviewer.currentPlacementMode = newMode;
        FiveFretNoteKeybindManager.InvokeUpdatePreviewer();
    }
}

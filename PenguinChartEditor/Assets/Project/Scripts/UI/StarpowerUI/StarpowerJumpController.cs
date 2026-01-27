using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarpowerJumpController : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private BeatsOrBarsSelector beatsOrBarsDropdown;
    [SerializeField] private Button applyJumpButton;
    
    private void Awake()
    {
        applyJumpButton.onClick.AddListener(ApplyJump);
    }
    
    private void ApplyJump()
    {
        if (!float.TryParse(inputField.text, out var inputFieldDuration))
        {
            return;
        }

        switch (beatsOrBarsDropdown.GetUnitSelectionMode())
        {
            case BeatsOrBarsSelector.JumpLengthOptions.Bars:
                SongTime.SongPositionTicks += Chart.SyncTrackInstrument.ConvertBarsToTicks(SongTime.SongPositionTicks, inputFieldDuration);
                break;
            case BeatsOrBarsSelector.JumpLengthOptions.Beats:
                SongTime.SongPositionTicks += (int)(inputFieldDuration * Chart.Resolution);
                break;
            default:
                throw new ArgumentException(
                    "Unsupported jump option in dropdown. Please add functionality for this mode in StarpowerJumpController.cs"
                    );
        }
        
        Chart.InPlaceRefresh();
    }
    
}
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarpowerJumpController : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Dropdown beatsOrBarsDropdown;
    [SerializeField] private Button applyJumpButton;

    enum JumpLengthOptions
    {
        Beats = 0,
        Bars = 1
    }
    
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

        switch ((JumpLengthOptions)beatsOrBarsDropdown.value)
        {
            case JumpLengthOptions.Bars:
                SongTime.SongPositionTicks += Chart.SyncTrackInstrument.ConvertBarsToTicks(SongTime.SongPositionTicks, inputFieldDuration);
                break;
            case JumpLengthOptions.Beats:
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
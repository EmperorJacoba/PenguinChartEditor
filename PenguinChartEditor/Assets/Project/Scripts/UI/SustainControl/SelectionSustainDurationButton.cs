using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SelectionSustainDurationButton : MonoBehaviour
{
    private Button sustainApplyButton;
    
    // All buttons apply bar lengths.
    [SerializeField] private float appliedBarDuration;
    [SerializeField] private bool isMaximumSustainLength;

    [SerializeField] private bool isSelection;

    private void Awake()
    {
        sustainApplyButton = GetComponent<Button>();
        
        sustainApplyButton.onClick.AddListener(ChangeRequiredSustainValue);
    }

    private void ChangeRequiredSustainValue()
    {
        if (isSelection) ApplyToSelection();
        else ApplyToPreviewer();
        
        Chart.InPlaceRefresh();
    }

    private void ApplyToSelection()
    {
        Chart.LoadedSustainableInstrument.SetSelectionSustain(
            isMaximumSustainLength ? SongTime.SongLengthTicks : appliedBarDuration);
    }

    private void ApplyToPreviewer()
    {
        Previewer.SetDefaultSustainLength(false,
            isMaximumSustainLength ? SongTime.SongLengthTicks : appliedBarDuration);
    }
}
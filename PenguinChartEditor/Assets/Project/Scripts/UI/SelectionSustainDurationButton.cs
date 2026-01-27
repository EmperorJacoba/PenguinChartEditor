using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SelectionSustainDurationButton : MonoBehaviour
{
    private Button sustainApplyButton;
    
    // All buttons apply bar lengths.
    [SerializeField] private float appliedBarDuration;

    private void Awake()
    {
        sustainApplyButton = GetComponent<Button>();
        
        sustainApplyButton.onClick.AddListener(ApplyToSelection);
    }

    private void ApplyToSelection()
    {
        Chart.LoadedSustainableInstrument.SetSelectionSustain(appliedBarDuration);
        Chart.InPlaceRefresh();
    }
}
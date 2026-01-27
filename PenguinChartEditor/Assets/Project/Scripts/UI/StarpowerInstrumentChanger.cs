using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarpowerInstrumentChanger : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown copyOrMoveDropdown;
    [SerializeField] private TMP_Dropdown instrumentDropdown;
    [SerializeField] private Button applyButton;
    [SerializeField] private List<Toggle> difficultyToggles;

    enum ActionMode
    {
        Move,
        Copy
    }

    ActionMode GetCurrentActionMode() => (ActionMode)copyOrMoveDropdown.value;

    InstrumentType GetTargetInstrument() => activeOptionPositions[instrumentDropdown.value];

    HashSet<DifficultyType> GetActiveDifficulties()
    {
        HashSet<DifficultyType> activeDiffs = new();
        for (int i = 0; i < difficultyToggles.Count; i++)
        {
            if (difficultyToggles[i].isOn && difficultyToggles[i].interactable) activeDiffs.Add((DifficultyType)i);
        }

        return activeDiffs;
    }
    
    private List<InstrumentType> activeOptionPositions;

    private void Start()
    {
        copyOrMoveDropdown.options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Move"),
            new TMP_Dropdown.OptionData("Copy")
        };

        SetDropdownOptions();
        applyButton.onClick.AddListener(ExecuteRequestedAction);
        instrumentDropdown.onValueChanged.AddListener((int _) => SetCheckmarkAvailability());
        
        SetCheckmarkAvailability();
    }

    private void SetCheckmarkAvailability()
    {
        var instrument = GetTargetInstrument();

        var activeDifficulties = Chart.GetActiveDifficulties(instrument);

        for (int toggleIndex = 0; toggleIndex < difficultyToggles.Count; toggleIndex++)
        {
            difficultyToggles[toggleIndex].interactable = activeDifficulties.Contains((DifficultyType)toggleIndex);
        }
    }

    private void ExecuteRequestedAction()
    {
        var mode = GetCurrentActionMode();
        var targetInstrument = GetTargetInstrument();
        var targetDifficulties = GetActiveDifficulties();

        switch (mode)
        {
            case ActionMode.Copy:
                Chart.StarpowerInstrument.CopySelectionTo(targetInstrument, targetDifficulties);
                break;
            case ActionMode.Move:
                Chart.StarpowerInstrument.MoveSelectionTo(targetInstrument, targetDifficulties);
                break;
            default:
                throw new ArgumentException(
                    "Tried to do an unaccounted for action. " +
                    "Please update StarpowerInstrumentChanger to address this new option.");
        }
        
        Chart.InPlaceRefresh();
    }

    private void SetDropdownOptions()
    {
        activeOptionPositions = Chart.GetLoadedInstrumentTypes().OrderBy(x => x).ToList();
        instrumentDropdown.options = activeOptionPositions
            .Select(x => new TMP_Dropdown.OptionData(MiscTools.Capitalize(x.ToString()))).ToList();
    }
}
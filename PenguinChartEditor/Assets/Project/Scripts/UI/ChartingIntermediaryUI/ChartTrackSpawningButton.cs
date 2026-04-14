using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChartTrackSpawningButton : MonoBehaviour
{
    [SerializeField] private DifficultyType difficulty;
    private ChartSceneSpawner parentSpawner;
    [SerializeField] private Button button;
    
    // Functionality of this is so minor that it doesn't warrant its own script.
    [SerializeField] private Toggle completionToggle;

    // Functionality of this should probably be in its own script but needs most of the same references as this one.
    [SerializeField] private Button copyDownButton;
    [SerializeField] private GameObject confirmationDialogPrefab;
    private static GameObject Canvas => GameObject.Find("Canvas");

    private void Start()
    {
        parentSpawner = transform.parent.GetComponent<ChartSceneSpawner>();
        UpdateButtonColors();
        
        button.onClick.AddListener(SpawnTrack);
        completionToggle.onValueChanged.AddListener(ChangeCompletionStatus);
        
        copyDownButton?.onClick.AddListener(PromptCopyAction);
    }

    private void SpawnTrack() => parentSpawner.SpawnTrack(difficulty);

    private void ChangeCompletionStatus(bool status)
    {
        Chart.Metadata.InstrumentCompletionStatuses[parentSpawner.GetInstrumentID(difficulty)] = status;
        UpdateButtonColors();
    }

    public void UpdateButtonColors()
    {
        var instrumentID = parentSpawner.GetInstrumentID(difficulty);
        
        completionToggle.interactable =
            Chart.IsInstrumentCreated(instrumentID);
        
        if (!completionToggle.interactable)
        {
            button.image.color = Color.white;
            return;
        }
        
        button.image.color =
            Chart.Metadata.InstrumentCompletionStatuses.ContainsKey(instrumentID) &&
            Chart.Metadata.InstrumentCompletionStatuses[instrumentID]
                ? Color.green
                : Color.yellow;
    }

    private void PromptCopyAction()
    {
        var targetDiff = parentSpawner.GetInstrumentID(difficulty - 1);
        
        if (Chart.IsInstrumentCreated(targetDiff))
        {
            var dialog = Instantiate(confirmationDialogPrefab, Canvas.transform).GetComponent<ConfirmationDialog>();
            dialog.Initialize($"This will overwrite {InstrumentMetadata.GetInstrumentName(targetDiff)}. This action cannot be undone. Overwrite?", ExecuteCopyAction);
            return;
        }
        
        ExecuteCopyAction();
    }

    private void ExecuteCopyAction()
    {
        Chart.DuplicateInstrumentToNewDifficulty(parentSpawner.GetInstrumentID(difficulty), parentSpawner.GetInstrumentID(difficulty - 1));
        parentSpawner.UpdateButton(difficulty - 1);
    }
}
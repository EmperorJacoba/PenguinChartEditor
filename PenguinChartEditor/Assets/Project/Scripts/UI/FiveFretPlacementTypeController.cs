using System;
using UnityEngine;
using UnityEngine.UI;

public class FiveFretPlacementTypeController : MonoBehaviour
{
    [SerializeField] private Button frettedActivator;
    [SerializeField] private Button openActivator;

    private void Awake()
    {
        frettedActivator.onClick.AddListener(ChangeToFretted);
        openActivator.onClick.AddListener(ChangeToOpen);
    }

    private static void ChangeToFretted() => FiveFretNotePreviewer.openNoteEditing = false;
    private static void ChangeToOpen() => FiveFretNotePreviewer.openNoteEditing = true;
}
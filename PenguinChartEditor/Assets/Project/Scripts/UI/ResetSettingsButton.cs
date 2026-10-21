using System;
using Penguin.Dialogs;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ResetSettingsButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ResetSettings);
    }

    private void ResetSettings()
    {
        var dialog = DialogManager.SpawnDialog<ConfirmationDialog>();
        dialog.Initialize(
            "Are you sure you want to reset your settings?", UserSettings.ResetSettings, 600, 250
            );
    }
}
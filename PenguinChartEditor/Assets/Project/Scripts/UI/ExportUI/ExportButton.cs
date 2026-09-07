using System;
using System.IO;
using Penguin.Dialogs;
using SFB;
using UnityEngine;
using UnityEngine.UI;

public class ExportButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Export);
    }

    private string lastDirName = "";

    private void Export()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel(
            "Open export directory",
            Chart.FolderPath,
            // Note: you must use true here. multiselect: false is broken on linux (segfaults)
            // No effect on windows.
            true 
        );

        if (paths.Length < 1) return;

        lastDirName = $"{paths[0]}/{Chart.Metadata.GenerateFolderName()}";

        if (Directory.Exists(lastDirName))
        {
            var dialog = DialogManager.SpawnDialog<ConfirmationDialog>();
            dialog.Initialize("Chart package already exists. Overwrite?", ConfirmExport);
            return;
        }
        
        ConfirmExport();
    }

    private void ConfirmExport()
    {
        Chart.ExportFile(lastDirName, ExportSettingsManager.instance.GetCurrentExportSettings());
    }
}
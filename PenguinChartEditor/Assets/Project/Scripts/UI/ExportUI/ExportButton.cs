using System;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UI;

public class ExportButton : MonoBehaviour
{
    private Button button;
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject overwriteDialog;

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
            false
        );

        if (paths.Length < 1) return;
        
        var artist = Chart.Metadata.SongInfo[Metadata.MetadataType.artist];
        var name = Chart.Metadata.SongInfo[Metadata.MetadataType.name];
        var charter = Chart.Metadata.SongInfo[Metadata.MetadataType.charter];

        var fileName = MiscTools.CleanFileName($"{artist} - {name} ({charter}");
        lastDirName = @"\\?\" + $"{paths[0]}/{fileName})";

        if (Directory.Exists(lastDirName))
        {
            var dialog = Instantiate(overwriteDialog, canvas.transform).GetComponent<ConfirmationDialog>();
            dialog.Initialize("File already exists. Overwrite?", ConfirmExport);
            return;
        }
        
        ConfirmExport();
    }

    private void ConfirmExport()
    {
        Chart.ExportFile(lastDirName);
    }
}
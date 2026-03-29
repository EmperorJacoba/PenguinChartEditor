using System.Collections.Generic;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StemSourceSelector : MonoBehaviour
{
    public StemType audioStemType;
    private string FormattedStem => audioStemType.ToString().Replace("_", " ");
    [SerializeField] private TMP_Text stemLabel;
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_InputField stemDisplayField;
    [SerializeField] private Button removeButton;

    private void Awake()
    {
        stemLabel.text = MiscTools.Capitalize(audioStemType.ToString().Replace("_", " ")) + ":";
        selectButton.onClick.AddListener(SetAudioStem);
        removeButton.onClick.AddListener(RemoveAudioStem);

        stemDisplayField.text = Chart.Metadata.StemPaths.GetValueOrDefault(audioStemType, "");
    }

    private void SetAudioStem()
    {
        var pathCandidates = 
            StandaloneFileBrowser.OpenFilePanel
            (
                $"Open audio track for {FormattedStem}", 
                $"{Chart.FolderPath}", 
                new[]
                {
                    new ExtensionFilter(
                        "Supported audio codecs", 
                        "opus", "ogg", "mp3", "wav", "flac"),
                }, 
                false
            );
        if (pathCandidates.Length < 1) return;

        stemDisplayField.text = 
            AudioManager.UpdateAudioStream(audioStemType, pathCandidates[0]) ? 
                pathCandidates[0] : 
                "ERROR LOADING FILE";
    }

    private void RemoveAudioStem()
    {
        AudioManager.RemoveStream(audioStemType);
        Destroy(this);
    }
}
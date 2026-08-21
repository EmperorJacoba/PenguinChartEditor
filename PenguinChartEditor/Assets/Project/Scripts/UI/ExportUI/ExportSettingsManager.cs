using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExportSettingsManager : MonoBehaviour
{
    public static ExportSettingsManager instance;

    [SerializeField] private DisabledApatheticToggleGroup chartFormatToggleGroup;
    [SerializeField] private DisabledApatheticToggleGroup audioFormatToggleGroup;
    [SerializeField] private Toggle zipPackageToggle;
    [SerializeField] private TMP_InputField kbpsInput;
    [SerializeField] private AudioTrackToggleManager audioTrackInclusionManager;
    [SerializeField] private InstrumentInclusionManager instrumentInclusionManager;
    
    private void Start()
    {
        LoadExportSettings(UserSettings.ReadExportSettingsFromDisk());
        instance = this;
    }

    private AudioFormat GetExportAudioFormat() =>
        audioFormatToggleGroup.activeToggle.GetComponent<AudioFormatToggle>().format;

    private ChartFormat GetExportChartFormat() =>
        chartFormatToggleGroup.activeToggle.GetComponent<ExportFormatToggle>().format;

    private bool ExportAsZipEnabled() => zipPackageToggle.isOn;

    private int GetKBPS()
    {
        if (int.TryParse(kbpsInput.text, out var kbps))
        {
            return kbps;
        }

        // Recommended opus is 80kbps. 320 for everything else. This is only if the input field is messed up for whatever reason
        return GetExportAudioFormat() == AudioFormat.opus ? 80 : 320;
    }

    private List<StemType> GetAudioInclusionStatuses()
    {
        return audioTrackInclusionManager.GetTrackInclusionStatuses().
            Where(x => x.Value).
            Select(x => x.Key).
            ToList();
    }

    private List<HeaderType> GetInstrumentTrackInclusionStatuses()
    {
        var includedIDs = new List<HeaderType>();
        foreach (var (instrumentType, includedDifficulties) in instrumentInclusionManager.GetActiveInstrumentTracks())
        {
            foreach (var diff in includedDifficulties.Where(diff => diff.Value))
            {
                includedIDs.Add(InstrumentMetadata.GetHeader(instrumentType, diff.Key));
            }
        }

        return includedIDs;
    }

    public ExportSettings GetCurrentExportSettings()
    {
        var exportSettings = new ExportSettings
        {
            audioTrackInclusion = GetAudioInclusionStatuses(),
            instrumentInclusion = GetInstrumentTrackInclusionStatuses(),
            audioQuality = GetKBPS(),
            audioFormat = GetExportAudioFormat(),
            chartFormat = GetExportChartFormat(),
            zip = ExportAsZipEnabled()
        };
        return exportSettings;
    }

    private void LoadExportSettings(ExportSettings exportSettings)
    {
        // Very lazy way to do this, sorry.
        var formatToggle = audioFormatToggleGroup
            .GetComponentsInChildren<AudioFormatToggle>().First(x => x.format == exportSettings.audioFormat);
        var chartToggle = chartFormatToggleGroup.GetComponentsInChildren<ExportFormatToggle>()
            .First(x => x.format == exportSettings.chartFormat);

        formatToggle.gameObject.GetComponent<Toggle>().isOn = true;
        chartToggle.gameObject.GetComponent<Toggle>().isOn = true;

        kbpsInput.text = exportSettings.audioQuality.ToString();
        zipPackageToggle.isOn = exportSettings.zip;

        // Intentionally not loading audio track/instrument track inclusion statuses as of right now.
    }
}

[Serializable]
public class ExportSettings
{
    // TODO: Read these two lists back on a file-basis, not on a program-basis. These are too volatile and will lead
    // to user rage and confusion if these are saved on a program-basis:
    // Example: user charts an expert track-only chart and then a full difficulty chart. Doesn't check settings. Will
    // be very confused when only expert exports on the second chart and will fall down forum/reddit/discord hell
    // trying to figure out why. Let's not do that.
    public List<StemType> audioTrackInclusion;
    public List<HeaderType> instrumentInclusion;
    
    public int audioQuality;
    public AudioFormat audioFormat;
    public ChartFormat chartFormat;
    public bool zip;
}
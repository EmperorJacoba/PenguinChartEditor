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
    
    private void Awake()
    {
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

    private HashSet<StemType> GetAudioInclusionStatuses()
    {
        return audioTrackInclusionManager.GetTrackInclusionStatuses().
            Where(x => x.Value).
            Select(x => x.Key).
            ToHashSet();
    }

    private HashSet<HeaderType> GetInstrumentTrackInclusionStatuses()
    {
        var includedIDs = new HashSet<HeaderType>();
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
}

public class ExportSettings
{
    public HashSet<StemType> audioTrackInclusion;
    public HashSet<HeaderType> instrumentInclusion;
    public int audioQuality;
    public AudioFormat audioFormat;
    public ChartFormat chartFormat;
    public bool zip;
}
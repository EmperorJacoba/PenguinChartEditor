using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ExportSettingsManager : MonoBehaviour
{
    public static ExportSettingsManager instance;

    [FormerlySerializedAs("formatToggleGroup")] [SerializeField] private ToggleGroup chartFormatToggleGroup;
    [SerializeField] private ToggleGroup audioFormatToggleGroup;
    [SerializeField] private Toggle zipPackageToggle;
    [SerializeField] private TMP_InputField kbpsInput;
    [SerializeField] private AudioTrackToggleManager audioTrackInclusionManager;
    [SerializeField] private InstrumentInclusionManager instrumentInclusionManager;
    
    private void Awake()
    {
        instance = this;
    }

    public AudioFormats GetExportAudioFormat()
    {
        print(audioFormatToggleGroup.AnyTogglesOn());
        var t = audioFormatToggleGroup.ActiveToggles();
        var f = t.FirstOrDefault();
        print(f is null);
        var o = f.GetComponent<AudioFormatToggle>();
        return o.format;
    }
        //audioFormatToggleGroup.ActiveToggles().FirstOrDefault()!.GetComponent<AudioFormatToggle>().format;

    public ChartFormats GetExportChartFormat() =>
        chartFormatToggleGroup.ActiveToggles().FirstOrDefault()!.GetComponent<ExportFormatToggle>().format;

    public bool ExportAsZipEnabled() => zipPackageToggle.isOn;

    public int GetKBPS()
    {
        if (int.TryParse(kbpsInput.text, out var kbps))
        {
            return kbps;
        }

        // Recommended opus is 80kbps. 320 for everything else. This is only if the input field is messed up for whatever reason
        return GetExportAudioFormat() == AudioFormats.opus ? 80 : 320;
    }

    public HashSet<StemType> GetAudioInclusionStatuses()
    {
        return audioTrackInclusionManager.GetTrackInclusionStatuses().
            Where(x => x.Value).
            Select(x => x.Key).
            ToHashSet();
    }

    public HashSet<HeaderType> GetInstrumentTrackInclusionStatuses()
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
}
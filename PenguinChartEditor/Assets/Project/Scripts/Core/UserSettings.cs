using System;
using System.IO;
using UnityEngine;

public class UserSettings
{
    #region User editable

    public enum SettingProperty
    {
        minSustainLength,
        calibration,
        defaultResolution,
        scrollSensitivity,
        sustainGapTicks,
        maximumSavedUndoActions
    }
    
    public void SetChartingSetting(SettingProperty property, int input)
    {
        switch (property)
        {
            case SettingProperty.minSustainLength:
                MinimumSustainLengthSeconds = input / 1000.0f;
                break;
            case SettingProperty.calibration:
                Calibration = input;
                break;
            case SettingProperty.defaultResolution:
                DefaultResolution = input;
                break;
            case SettingProperty.scrollSensitivity:
                ScrollSensitivity = input;
                break;
            case SettingProperty.sustainGapTicks:
                SustainGapTicks = input;
                break;
            case SettingProperty.maximumSavedUndoActions:
                MaximumSavedUndoActions = input;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, null);
        }
    }
    
    public int GetChartingSetting(SettingProperty property)
    {
        return property switch
        {
            SettingProperty.minSustainLength => (int)(MinimumSustainLengthSeconds * 1000),
            SettingProperty.calibration => Calibration,
            SettingProperty.defaultResolution => DefaultResolution,
            SettingProperty.scrollSensitivity => ScrollSensitivity,
            SettingProperty.sustainGapTicks => SustainGapTicks,
            SettingProperty.maximumSavedUndoActions => MaximumSavedUndoActions,
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
    }
    
    public float MinimumSustainLengthSeconds = 0.2f;

    // Calibration is a lie. It is a lie told by the AudioManager to SongTime about where the song actually is.
    // In AudioManager, the internal positions of each of the audio streams are offset by {Calibration} seconds, and when
    // SongTime polls the audio position, AudioManager offsets it back the opposite direction. Effectively, the internal
    // audio position of the audio is shifted away the shown/true/expected position. On certain machines with high audio lag
    // (due to ancient tech or driver lag or whatever), there is a delay when playing the audio (audio plays significantly later than what is expected.
    // Obviously bad for rhythm games. This calibration "lie" allows the audio position to be offset by that delay so that
    // in effect there is no delay. The delay is usually so small it doesn't matter much, especially when tempo mapping
    // is de facto required anyway and done to the waveform, and charting is done to the tempo map. Older/slower machines
    // have a delay issue, which is what this fixes.
    // A negative offset will "push" the audio further into the chart, and a positive value will "pull" it back.
    public int Calibration = 0;

    /// <summary>
    /// Value autofilled into "Resolution" box upon new song creation.
    /// </summary>
    public int DefaultResolution = 192;
    public int ScrollSensitivity = 10;
    public int SustainGapTicks = 50;
    public int MaximumSavedUndoActions = 1024;
    
    #endregion
    
    #region User editable, not yet implemented
    
    public bool ShowSidebarSections = true;
    public bool LeftyFlip;
    public bool OpenNoteAsFret = false;
    public string MetadataImagePaths => $"{Chart.FolderPath}";
    
    /// ---
    /// Should these be settings/features?
    ///
    
    public float TimeToCullObjects = 5.0f;
    public float ButtonScrollSensitivity = 0.025f;
    
    // used only for .chart [Song] headers, no bearing on .ini files
    // in milliseconds!!
    public int DefaultPreviewLength = 3000;
    
    /// ---
    
    #endregion

    #region In-scene settings

    public bool ExtendedSustains = true; 
    public bool SoloPlacingAllowed = true;
    
    // Doesn't work properly as of current.
    public float userSetHighwayLength = 75.0f;

    #endregion
    
    #region Disk

    private static string SettingsDirectoryPath => Path.Combine(Application.persistentDataPath, "settings");
    private static string SettingsFilePath => Path.Combine(SettingsDirectoryPath, "settings.json");
    private static string CosmeticSettingsFilePath => Path.Combine(SettingsDirectoryPath, "cosmetics.json");
    private static string CustomKeybindsFilePath => Path.Combine(SettingsDirectoryPath, "inputs.json");
    
    public void SaveSettingsToDisk()
    {
        if (!Directory.Exists(SettingsDirectoryPath))
        {
            Directory.CreateDirectory(SettingsDirectoryPath);
        }
        File.WriteAllText(SettingsFilePath, JsonUtility.ToJson(this));
        File.WriteAllText(CustomKeybindsFilePath, Chart.inputMap.asset.ToJson());
        var cosmetics = new UniversalCosmeticSettings();
        cosmetics.WriteToDisk(CosmeticSettingsFilePath);
    }

    public static UserSettings ReadFromDisk()
    {
        if (File.Exists(CustomKeybindsFilePath))
        {
            Chart.inputMap.Disable();
            Chart.inputMap.asset.LoadFromJson(File.ReadAllText(CustomKeybindsFilePath));
            Chart.inputMap.Enable();
        }
        
        if (File.Exists(SettingsFilePath))
        {
            return (UserSettings)JsonUtility.FromJson(File.ReadAllText(SettingsFilePath), typeof(UserSettings));
        }
        
        return new UserSettings();
    }

    public static void LoadCosmeticSettings()
    {
        UniversalCosmeticSettings.ApplySavedSettings(CosmeticSettingsFilePath);
    }
    
    #endregion
}

// Consider this a gathering mechanism - get everything in one place, then serialize it.
// Works well with unity objects via JsonUtility in case they are ever required (which is likely as cosmetic options
// increase in future). While I would like a cleaner functional solution I think this is the best way to keep this
// future-aware.
internal class UniversalCosmeticSettings
{
    // public so that JsonUtility can serialize
    public float hyperspeed = Waveform.ShrinkFactor;
    public float amplitude = Waveform.Amplitude;
    public float playSpeed = AudioManager.AudioSpeed;
    public float highwayLength = Highway.highwayLength;
    public double masterVolume = AudioManager.MasterVolume;
    public float metronomeVolume = AudioManager.GetSFXVolume(AudioManager.SFX.metronome);
    public float clapVolume = AudioManager.GetSFXVolume(AudioManager.SFX.clap);

    public void WriteToDisk(string filePath)
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
        }
        
        File.WriteAllText(filePath, JsonUtility.ToJson(this));
    }

    public static void ApplySavedSettings(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var savedData =
            (UniversalCosmeticSettings)JsonUtility.FromJson(
                File.ReadAllText(filePath),
                typeof(UniversalCosmeticSettings)
            );

        Waveform.ShrinkFactor = savedData.hyperspeed;
        Waveform.Amplitude = savedData.amplitude;
        AudioManager.AudioSpeed = savedData.playSpeed;
        Highway.highwayLength = savedData.highwayLength;
        AudioManager.MasterVolume = savedData.masterVolume;
        AudioManager.SetSFXVolume(AudioManager.SFX.metronome, savedData.metronomeVolume);
        AudioManager.SetSFXVolume(AudioManager.SFX.clap, savedData.clapVolume);
    }
} 

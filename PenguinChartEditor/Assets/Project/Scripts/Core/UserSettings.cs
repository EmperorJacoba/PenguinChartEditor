using System.IO;
using UnityEngine;
using SimpleJSON;

public class UserSettings
{
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

    /// <summary>
    /// Is the chart mode currently using extended sustains?
    /// </summary>
    public bool ExtSustains { get; set; } = true; // Note: must be able to switch readily
    // Why? -> No ExtSus means that sustain gap applies automatically even if not cleanly terminated
    
    public int SustainGapTicks { get; set; } = 50;

    public int ScrollSensitivity { get; set; } = 10;

    public float ButtonScrollSensitivity { get; set; } = 0.025f;

    // used only for .chart [Song] headers, no bearing on .ini files
    // in milliseconds!!
    public int DefaultPreviewLength { get; set; } = 3000;

    public bool OpenNoteAsFret { get; set; } = false;

    public bool SoloPlacingAllowed { get; set; } = true;

    public bool ShowSidebarSections { get; set; } = true;

    public float TimeToCullObjects { get; set; } = 5.0f;

    public int MaximumSavedUndoActions { get; set; } = 1024;

    public string MetadataImagePaths => $"{Chart.FolderPath}";

    public float userSetHighwayLength = 75.0f;
    
    /// <summary>
    /// Is the user using lefty flip mode?
    /// </summary>
    public bool LeftyFlip { get; set; }

    private static string SettingsFilePath => Path.Combine(Application.persistentDataPath, "settings", "settings.json");
    
    public void SaveSettings()
    {
        File.WriteAllText(SettingsFilePath, JsonUtility.ToJson(this));
    }

    public static UserSettings ReadFromDisk()
    {
        if (File.Exists(SettingsFilePath))
        {
            return (UserSettings)JsonUtility.FromJson(File.ReadAllText(SettingsFilePath), typeof(UserSettings));
        }

        return new UserSettings();
    }
}

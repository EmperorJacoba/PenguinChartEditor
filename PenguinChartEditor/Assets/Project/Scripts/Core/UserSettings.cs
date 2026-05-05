using UnityEngine;

public static class UserSettings
{
    public const float MINIMUM_SUSTAIN_LENGTH_SECONDS = 0.2f;

    // Calibration is a lie. It is a lie told by the AudioManager to SongTime about where the song actually is.
    // In AudioManager, the internal positions of each of the audio streams are offset by {Calibration} seconds, and when
    // SongTime polls the audio position, AudioManager offsets it back the opposite direction. Effectively, the internal
    // audio position of the audio is shifted away the shown/true/expected position. However, on certain machines with high audio lag
    // (due to ancient tech or driver lag or whatever), there is a delay when playing the audio (audio plays significantly later than what is expected.
    // Obviously bad for rhythm games. This calibration "lie" allows the audio position to be offset by that delay so that
    // in effect there is no delay. The delay is usually so small it doesn't matter much, especially when tempo mapping
    // is de facto required anyway and done to the waveform, and charting is done to the tempo map. Older/slower machines
    // have a delay issue, which is what this fixes.
    // A negative offset will "push" the audio further into the chart, and a positive value will "pull" it back.
    public static int Calibration { get; set; } = 0;

    /// <summary>
    /// Is the user using lefty flip mode?
    /// </summary>
    public static bool LeftyFlip { get; set; }

    /// <summary>
    /// Value autofilled into "Resolution" box upon new song creation.
    /// </summary>
    public static int DefaultResolution = 192;

    /// <summary>
    /// Is the chart mode currently using extended sustains?
    /// </summary>
    public static bool ExtSustains { get; set; } = true; // Note: must be able to switch readily
    // Why? -> No ExtSus means that sustain gap applies automatically even if not cleanly terminated

    /// <summary>
    /// The required distance between the end of a sustained note and the beginning of any next note, in milliseconds.
    /// <para>Example: SustainGap is 50 milliseconds -> Gap between end of sustained note and next note is 50ms, converted approximately to tick time.</para>
    /// </summary>
    public static int SustainGap { get; set; }

    public static int SustainGapTicks { get; set; } = 50;

    public static int ScrollSensitivity { get; set; } = 10;

    public static float ButtonScrollSensitivity { get; set; } = 0.025f;

    // used only for .chart [Song] headers, no bearing on .ini files
    // in milliseconds!!
    public static int DefaultPreviewLength { get; set; } = 3000;

    public static bool OpenNoteAsFret { get; set; } = false;

    public static bool SoloPlacingAllowed { get; set; } = true;

    public static bool ShowSidebarSections { get; set; } = true;

    public static float TimeToCullObjects { get; set; } = 5.0f;

    public static int MaximumSavedUndoActions { get; set; } = 1024;

    public static string MetadataImagePaths => $"{Chart.FolderPath}";

    public static float userSetHighwayLength = 75.0f;
}

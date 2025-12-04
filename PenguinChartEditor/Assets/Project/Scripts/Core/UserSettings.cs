using UnityEngine;

public static class UserSettings
{
    public const float MINIMUM_SUSTAIN_LENGTH_SECONDS = 0.2f;

    /// <summary>
    /// The offset at which audio should be played in relation to video in milliseconds.
    /// <para>Example: Calibration = 50 means that audio will be offset 50 milliseconds late in-editor.</para>
    /// </summary>
    public static int Calibration { get; set; }

    /// <summary>
    /// Is the user using lefty flip mode?
    /// </summary>
    public static bool LeftyFlip { get; set; }

    /// <summary>
    /// Value autofilled into "Resolution" box upon new song creation.
    /// </summary>
    public static int DefaultResolution = 320;

    /// <summary>
    /// Is the chart mode currently using extended sustains?
    /// </summary>
    public static bool ExtSustains { get; set; } = true;  // Note: must be able to switch readily
                                                   // Why? -> No ExtSus means that sustain gap applies automatically even if not cleanly terminated

    /// <summary>
    /// The required distance between the end of a sustained note and the beginning of any next note, in milliseconds.
    /// <para>Example: SustainGap is 50 milliseconds -> Gap between end of sustained note and next note is 50ms, converted approximately to tick time.</para>
    /// </summary>
    public static int SustainGap { get; set; }
    // Note on sustain gaps:
    // Sustain gap is stored in milliseconds, but works in tick time
    // Distance between one note sustain and following note must be converted to ticks from ms, based on tempo
    // Formula from chart file format specifications:
    // (tickEnd - tickStart) / resolution * 60 / BPM
    // With current vars in scripts:
    // (note2.TickPosition - note1.TickPosition) / ChartMetadata.Resolution * 60 / beatline.Tempo

    public static int SustainGapTicks { get; set; } = 50;

    public static int ScrollSensitivity { get; set; } = 10;

    public static float ButtonScrollSensitivity { get; set; } = 0.025f;

    // used only for .chart [Song] headers, no bearing on .ini files
    // in milliseconds!!
    public static float DefaultPreviewLength { get; set; } = 3000.0f;
}

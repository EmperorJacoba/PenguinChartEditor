using UnityEngine;

public class ChartMetadata : MonoBehaviour
{
    /// <summary>
    /// Name of the loaded song.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Name of the loaded song artist.
    /// </summary>
    public string Artist { get; set; }

    /// <summary>
    /// Name of the loaded song's album.
    /// </summary>
    public string Album { get; set; }

    /// <summary>
    /// Genre of the loaded song
    /// </summary>
    public string Genre { get; set; }

    /// <summary>
    /// Release year of the loaded song. 
    /// </summary>
    public string Year { get; set; }

    /// <summary>
    /// Name of the charter(s) who have worked on the loaded song.
    /// </summary>
    public string Charter { get; set; }
    

    /// <summary>
    /// Length of the song in milliseconds.
    /// </summary>
    public int SongLength { get; } // set through CalculateSongLength() in audio parser?
                                    // Note: set to the length of the longest audio stem


    /// All of these values store difficulties in a value from 0-6, although values higher than 6 are allowed for some niche CH uses.
    public int DiffBand { get; set; }
    public int DiffGuitar { get; set; }
    public int DiffGuitarCoop { get; set; }
    public int DiffRhythm { get; set; }
    public int DiffBass { get; set; }
    public int DiffDrums { get; set; }
    public int DiffDrumsReal { get; set; }
    public int DiffEliteDrums { get; set; } // named as such to conform with requested diff format in elite drums specifications
    public int DiffKeys { get; set; }
    public int DiffKeysReal { get; set; } // for planned Pro Keys
    public int DiffGHL { get; set; }
    public int DiffBassGHL { get; set; }
    public int DiffVocals { get; set; } // for planned Vocals
    public int DiffHarmonies { get; set; }

    /// <summary>
    /// Time to start in-game song preview in milliseconds.
    /// </summary>
    public int PreviewStartTime { get; } // set through CalculatePreviewTime() -> take HH:MM:SS input from user, translate into ms
    
    /// <summary>
    /// Charter Icon ID or Source ID as listed in the Clone Hero Icons and Sources gitlab repo.
    /// <para> Link: https://gitlab.com/clonehero/sources </para>
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// Text meant to be shown in the "loading" screen (instrument/difficulty selection screen).
    /// </summary>
    public string LoadingPhrase { get; set; }

    /// <summary>
    /// Position of the song in the album's ordering .
    /// </summary>
    public int AlbumTrack { get; set; }

    /// <summary>
    /// Position of the song in a setlist .
    /// </summary>
    public int PlaylistTrack { get; set; }

    /// <summary>
    /// Is the chart a modchart?
    /// </summary>
    public bool Modchart { get; set; }

    /// <summary>
    /// The time at which the video begins playing in the track, in milliseconds.
    /// </summary>
    public int VideoStartTime { get; set; }

    /// <summary>
    /// Number of ticks per quarter note (VERY IMPORTANT FOR SONG RENDERING)
    /// </summary>
    public int Resolution { get; set; }

    // Add album cover for exports
        // Save as file explorer directory?
    // Add video for exports
        // Save as file explorer directory?

    // Attach to a GameObject to store metadata, create ini file from the GameObject

}

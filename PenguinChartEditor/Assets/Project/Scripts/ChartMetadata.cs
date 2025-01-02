using System;
using UnityEngine;
using UnityEngine.UI;

public class ChartMetadata : MonoBehaviour
{
    /// <summary>
    /// Name of the loaded song.
    /// </summary>
    public string Song_name { get; set; }

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
    public int Song_length { get; private set; } // set through CalculateSongLength() in audio parser?
                                    // Note: set to the length of the longest audio stem


    // All of these values store difficulties in a value from 0-6, although values higher than 6 are allowed for some niche CH uses.
    // Set up these values in the CHART tab - no sense setting them up when you don't have the tracks charted yet!
    public int Diff_band { get; set; }
    public int Diff_guitar { get; set; }
    public int Diff_guitar_coop { get; set; }
    public int Diff_rhythm { get; set; }
    public int Diff_bass { get; set; }
    public int Diff_drums { get; set; }
    public int Diff_drums_real { get; set; }
    public int Diff_elite_drums { get; set; } // named as such to conform with requested diff format in elite drums specifications
    public int Diff_keys { get; set; }
    public int Diff_keys_real { get; set; } // for planned Pro Keys
    public int Diff_ghl { get; set; }
    public int Diff_bass_ghl { get; set; }
    public int Diff_vocals { get; set; } // for planned Vocals
    public int Diff_vocals_harm { get; set; }

    /// <summary>
    /// Time to start in-game song preview in milliseconds.
    /// </summary>
    public int Preview_start_time { get; private set;} // set through CalculatePreviewTime() -> take HH:MM:SS input from user, translate into ms
    
    /// <summary>
    /// Charter Icon ID or Source ID as listed in the Clone Hero Icons and Sources gitlab repo.
    /// <para> Link: https://gitlab.com/clonehero/sources </para>
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// Text meant to be shown in the "loading" screen (instrument/difficulty selection screen).
    /// </summary>
    public string Loading_phrase { get; set; }

    /// <summary>
    /// Position of the song within the album's track ordering
    /// </summary>
    public string Album_track { get; set; } // String to work with text input

    /// <summary>
    /// Position of the song in a setlist .
    /// </summary>
    public string Playlist_track { get; set; } // String to work with text input

    /// <summary>
    /// Is the chart a modchart?
    /// </summary>
    // public bool Modchart { get; set; } Not going to worry about this after all

    /// <summary>
    /// The time at which the video begins playing in the track, in milliseconds.
    /// </summary>
    public int Video_start_time { get; set; }

    /// <summary>
    /// Number of ticks per quarter note (VERY IMPORTANT FOR SONG RENDERING)
    /// </summary>
    public int Resolution { get; set; }

    // Add album cover for exports
        // Save as file explorer directory?
    // Add video for exports
        // Save as file explorer directory?

    // Attach to a GameObject to store metadata, create ini file from the GameObject

    // Use to setup DDOL & Data persistence
    public static ChartMetadata metadata;
    private void Awake()
    {
        metadata = this;
        DontDestroyOnLoad(gameObject);
    }
}


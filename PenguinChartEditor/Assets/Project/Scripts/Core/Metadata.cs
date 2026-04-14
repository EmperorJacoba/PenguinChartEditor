using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Metadata
{
    public string CoverPath { get; set; } = "";
    public string BackgroundPath { get; set; } = "";

    /// <summary>
    /// Stores valid song metadata fields.
    /// </summary>
    public enum MetadataType
    {
        name,
        artist,
        album,
        genre,
        year,
        charter,
        icon,
        loading_phrase,
        album_track,
        playlist_track,
        video_start_time,
    }

    public delegate void PreviewStartTimeUpdatedDel();
    public PreviewStartTimeUpdatedDel PreviewStartTimeUpdated;

    public float PreviewStartTime
    {
        get => _pst;
        set
        {
            if (value < 0) value = 0;

            if (Chart.ChartLoading)
            {
                _pst = value;
                return;
            }
            
            if (value > AudioManager.SongLength) value = (float)AudioManager.SongLength;
            
            _pst = value;
            PreviewStartTimeUpdated?.Invoke();
        }
    }
    
    private float _pst = 0;
    
    public Dictionary<MetadataType, string> SongInfo = new();

    // All of these values store difficulties in a value from 0-6, although values higher than 6 are allowed for some niche CH uses.
    // Set up these values in the CHART tab - no sense setting them up when you don't have the tracks charted yet!
    /// <summary>
    /// Stores valid instrument difficulties.
    /// </summary>
    public enum InstrumentDifficultyIdentifier
    {
        diff_band,
        diff_guitar,
        diff_guitar_coop,
        diff_rhythm,
        diff_bass,
        diff_drums,
        diff_drums_real,
        diff_elite_drums,
        diff_keys,
        diff_keys_real,
        diff_guitarghl,
        diff_bassghl,
        diff_coopghl,
        diff_rhythmghl,
        diff_vocals,
        diff_vocals_harm
    }

    public Dictionary<InstrumentDifficultyIdentifier, int> Difficulties = new();

    public InstrumentDifficultyIdentifier MatchInstrumentToDiffID(InstrumentType instrumentType)
    {
        return ((int)instrumentType) switch
        {
            < 10 => InstrumentDifficultyIdentifier.diff_band,
            < 20 => InstrumentDifficultyIdentifier.diff_guitar,
            < 30 => InstrumentDifficultyIdentifier.diff_guitar_coop,
            < 40 => InstrumentDifficultyIdentifier.diff_bass,
            < 50 => InstrumentDifficultyIdentifier.diff_rhythm,
            < 60 => InstrumentDifficultyIdentifier.diff_keys,
            < 110 => InstrumentDifficultyIdentifier.diff_drums,
            < 120 => InstrumentDifficultyIdentifier.diff_elite_drums,
            < 1010 => InstrumentDifficultyIdentifier.diff_guitarghl,
            < 1020 => InstrumentDifficultyIdentifier.diff_bassghl,
            < 1030 => InstrumentDifficultyIdentifier.diff_coopghl,
            < 1040 => InstrumentDifficultyIdentifier.diff_rhythmghl,
            < 10010 => InstrumentDifficultyIdentifier.diff_vocals,
            < 10020 => InstrumentDifficultyIdentifier.diff_vocals_harm,
            _ => throw new ArgumentException(
                $"Tried to get invalid/uninitialized instrument type." +
                $" Please ensure that InstrumentType, InstrumentCategory, " +
                $"HeaderType, and InstrumentMetadata (and InstrumentIconMatchup) " +
                $"are properly updated to support your new instrument. " +
                $"Check Scripts/Core/CommonTypes.cs for more information.")
        };
    }

    public int GetDifficultyRating(InstrumentType instrumentType)
    {
        var diff_id = MatchInstrumentToDiffID(instrumentType);
        if (!Difficulties.ContainsKey(diff_id))
        {
            Difficulties[diff_id] = 0;
        }

        return Difficulties[diff_id];
    }

    public void SetDifficultyRating(InstrumentType instrumentType, int rating)
    {
        var diff_id = MatchInstrumentToDiffID(instrumentType);
        rating = Mathf.Max(0, rating);
        
        Difficulties[diff_id] = rating;
    }

    public Dictionary<StemType, string> StemPaths = new();

    public Dictionary<HeaderType, bool> InstrumentCompletionStatuses
    {
        get
        {
            // This will likely not be initialized most of the time (no support in .mid, .chart, etc.), so this is just
            // a foolproof way of initializing the buttons if loading a file (although Penguin is meant to be used as a tool
            // from 0% - 100%)
            _ics ??= Chart.Instruments.Select(x => x.InstrumentID).ToDictionary(x => x, x => false);
            return _ics;
        }
        set
        {
            _ics = value;
        }
    }

    private Dictionary<HeaderType, bool> _ics;

}
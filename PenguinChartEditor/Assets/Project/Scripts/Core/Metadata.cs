using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Metadata
{
    public string CoverPath { get; set; } = "";
    public string BackgroundPath { get; set; } = "";
    
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

    private const string QUOTES_STRING = "\"";
    private const string YEAR_COMMA = ", ";
    private const float MS_TO_SECONDS_CONVERSION = 1000.0f;

    public Metadata()
    {
        foreach (MetadataType key in Enum.GetValues(typeof(MetadataType)))
        {
            SongInfo[key] = "";
        } 
    }
    
    public Metadata(SongDataGroup songEventGroup)
    {
        Debug.LogWarning("Reading metadata from .chart internal data. .chart metadata is less rich than .ini data, " +
                         "and may not accurately reflect this song's information.");
        
        foreach (var kvp in songEventGroup.data)
        {
            if (!Enum.TryParse(typeof(MetadataType), kvp.Key, true, out var iniFormattedKey)) continue;
            
            var formattedValue = kvp.Value.Replace(QUOTES_STRING, "").Replace(YEAR_COMMA, "");
            SongInfo.Add((MetadataType)iniFormattedKey, formattedValue);
        }
    }

    private const string RESOLUTION_IDENTIFIER = "Resolution";
    private const string PREVIEW_START_TIME_IDENTIFIER = "PreviewStartTime";
    private const string BACKGROUND_PATH_IDENTIFIER = "BackgroundPath";
    private const string COVER_PATH_IDENTIFIER = "CoverPath";

    private enum MetadataSectionIDs
    {
        @base = 0,
        song = 1,
        diff = 2,
        completion = 3,
        paths = 4,
        audioVol = 5,
        muteStates = 6,
        soloStates = 7
    }

    public Metadata(List<PenguinEventSection> penguinFormattedEvents)
    {
        foreach (var section in penguinFormattedEvents)
        {
            switch ((MetadataSectionIDs)section.id)
            {
                case MetadataSectionIDs.@base:
                {
                    ParseBasicGroup(section);
                    break;
                }
                case MetadataSectionIDs.song:
                {
                    Func<string, MetadataType> conversionFunc = Enum.Parse<MetadataType>;
                    SongInfo = DeserializeDictionary<MetadataType, string>(section.lines, conversionFunc);
                    break;
                }
                case MetadataSectionIDs.diff:
                {
                    Func<string, InstrumentDifficultyIdentifier> conversionFunc = Enum.Parse<InstrumentDifficultyIdentifier>;
                    Difficulties = DeserializeDictionary<InstrumentDifficultyIdentifier, int>(section.lines, conversionFunc);
                    break;
                }
                case MetadataSectionIDs.completion:
                {
                    Func<string, HeaderType> conversionFunc = Enum.Parse<HeaderType>;
                    InstrumentCompletionStatuses = DeserializeDictionary<HeaderType, bool>(section.lines, conversionFunc);
                    break;
                }
                case MetadataSectionIDs.paths:
                {
                    Func<string, StemType> conversionFunc = Enum.Parse<StemType>;
                    StemPaths = DeserializeDictionary<StemType, string>(section.lines, conversionFunc);
                    break;
                }
                case MetadataSectionIDs.audioVol:
                {
                    Func<string, StemType> conversionFunc = Enum.Parse<StemType>;
                    loadedAudioVols = DeserializeDictionary<StemType, float>(section.lines, conversionFunc);
                    break;
                }
                case MetadataSectionIDs.muteStates:
                {
                    Func<string, StemType> conversionFunc = Enum.Parse<StemType>;
                    loadedMuteStates = DeserializeDictionary<StemType, bool>(section.lines, conversionFunc);
                    break;
                }
                case MetadataSectionIDs.soloStates:
                {
                    Func<string, StemType> conversionFunc = Enum.Parse<StemType>;
                    loadedSoloStates = DeserializeDictionary<StemType, bool>(section.lines, conversionFunc);
                    break;
                }
                default:
                {
                    Debug.LogWarning($"Skipping unknown identifier {section.id}");
                    break;
                }
            }
        }
    }

    private void ParseBasicGroup(PenguinEventSection section)
    {
        var parts = section.lines.Select(line => line.Trim().Split(" = "));

        foreach (var line in parts)
        {
            var val = line[1];
            switch (line[0])
            {
                case RESOLUTION_IDENTIFIER:
                {
                    Chart.Resolution = int.Parse(val);
                    break;
                }
                case PREVIEW_START_TIME_IDENTIFIER:
                {
                    PreviewStartTime = float.Parse(val);
                    break;
                }
                case COVER_PATH_IDENTIFIER:
                {
                    CoverPath = val;
                    break;
                }
                case BACKGROUND_PATH_IDENTIFIER:
                {
                    BackgroundPath = val;
                    break;
                }
                default:
                {
                    Debug.LogWarning($"Skipped unrecognized identifier {line[0]}");
                    break;
                }
            }
        }
    }

    private static readonly List<string> ignoredData = new List<string>()
    {
       "song_length"
    };
    
    public Metadata(IniDataGroup iniDataGroup)
    {
        foreach (var kvp in iniDataGroup.data)
        {
            if (Enum.TryParse(typeof(MetadataType), kvp.Key, true, out var formattedKey))
            {
                SongInfo.Add((MetadataType)formattedKey, kvp.Value);
            }
            else if (Enum.TryParse(typeof(InstrumentDifficultyIdentifier), kvp.Key, true, out var formattedInstrumentDiff))
            {
                if (!int.TryParse(kvp.Value, out int instrumentDifficulty)) continue;
                if (instrumentDifficulty < 0) continue;
                
                Difficulties.Add((InstrumentDifficultyIdentifier)formattedInstrumentDiff, instrumentDifficulty);
            }
            else if (kvp.Key.ToLower().Contains("preview_start_time"))
            {
                if (int.TryParse(kvp.Value, out var startTimeMs))
                {
                    PreviewStartTime = startTimeMs / MS_TO_SECONDS_CONVERSION;
                }
            }
            else
            {
                if (ignoredData.Contains(kvp.Key)) continue;
                
                Debug.LogWarning($"Could not parse .ini key \"{kvp.Key}\"");
            }
        }
    }
    
    private static List<string> SerializeDictionary<TKey, TValue>(
        int identifier,
        Dictionary<TKey, TValue> dictionary, 
        int level
    ) where TKey : Enum
    {
        var mainIndent = string.Concat(Enumerable.Repeat("\t", level + 1));
        var curlyIndent = string.Concat(Enumerable.Repeat("\t", level));

        List<string> output = new List<string>();
        
        output.Add($"{curlyIndent}{identifier}");
        output.Add($"{curlyIndent}" + "{");
        
        output.AddRange(
            dictionary.Where(x => Convert.ToString(x.Value) != "").
                Select(element => $"{mainIndent}{Convert.ToInt32(element.Key)} = {element.Value}")
            );
        
        output.Add($"{curlyIndent}" + "}");

        return output;
    }

    private static Dictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(string[] lines, Func<string, TKey> keyConversionMethod)
    {
        return
            lines.Select(
                line => line.Trim().Split(" = ", 2)
                ).ToDictionary(
                parts => 
                    keyConversionMethod(parts[0]), 
                parts => 
                    (TValue)Convert.ChangeType(parts[1], typeof(TValue)
                    )
                );
    }
    
    public List<string> ToPenguinFormat()
    {
        var list = new List<string>
        {
            $"{(int)HeaderType.Song}",
            "{",
        };
        
        list.Add($"\t{(int)MetadataSectionIDs.@base}");
        list.Add("\t{");
        list.Add($"\t\t{RESOLUTION_IDENTIFIER} = {Chart.Resolution}");
        list.Add($"\t\t{PREVIEW_START_TIME_IDENTIFIER} = {PreviewStartTime}");
        
        if (CoverPath != "")
        {
            list.Add($"\t\t{COVER_PATH_IDENTIFIER} = {CoverPath}");
        }

        if (BackgroundPath != "")
        {
            list.Add($"\t\t{BACKGROUND_PATH_IDENTIFIER} = {BackgroundPath}");
        }
        
        list.Add("\t}");
        
        list.AddRange(SerializeDictionary((int)MetadataSectionIDs.song, SongInfo, 1));
        list.AddRange(SerializeDictionary((int)MetadataSectionIDs.diff, Difficulties, 1));
        list.AddRange(SerializeDictionary((int)MetadataSectionIDs.completion, InstrumentCompletionStatuses, 1));
        list.AddRange(SerializeDictionary((int)MetadataSectionIDs.paths, StemPaths, 1));
        list.AddRange(SerializeDictionary((int)MetadataSectionIDs.audioVol, AudioManager.GetStemVolumes(), 1));
        list.AddRange(SerializeDictionary((int)MetadataSectionIDs.muteStates, AudioManager.GetStemMuteStates(), 1));
        list.AddRange(SerializeDictionary((int)MetadataSectionIDs.soloStates, AudioManager.GetStemSoloedStates(), 1));
        list.Add("}");
        return list;
    }

    public Dictionary<StemType, float> loadedAudioVols = null;
    public Dictionary<StemType, bool> loadedMuteStates = null;
    public Dictionary<StemType, bool> loadedSoloStates = null;
    
    public string GenerateFolderName()
    {
        var artist = Chart.Metadata.SongInfo[MetadataType.artist];
        var name = Chart.Metadata.SongInfo[MetadataType.name];
        var charter = Chart.Metadata.SongInfo[MetadataType.charter];

        return MiscTools.CleanFileName($"{artist} - {name} ({charter})");
    }

    public string GeneratePenguinFileName()
    {
        var artist = Chart.Metadata.SongInfo[MetadataType.artist];
        var name = Chart.Metadata.SongInfo[MetadataType.name];

        return MiscTools.CleanFileName($"{artist} - {name}");
    }
}
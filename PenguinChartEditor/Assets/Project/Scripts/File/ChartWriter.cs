using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class ChartWriter
{
    private const int CHART_FIELDS_ENUM_START_POINT = 0;
    private const int CHART_FIELDS_ENUM_END_POINT = 5;
    private const string CLOSING_GROUP_CHAR = "}";
    private const string OFFSET = "Offset = 0";

    public static void WriteChart()
    {
        var dotChartLines = GenerateData();
        
        // detect changes in metadata to avoid doing this frequently
        var iniLines = WriteIni();
        
        if (!File.Exists($"{Chart.ChartPath}"))
        {
            File.Create(Chart.ChartPath);
        }
        
        string iniPath = $"{Chart.FolderPath}\\song.ini";

        if (!File.Exists(iniPath))
        {
            File.Create(iniPath);
        }

        File.WriteAllLines(Chart.ChartPath, dotChartLines);
        File.WriteAllLines(iniPath, iniLines);

        Chart.Log($"Saved. {Chart.ChartPath}");
    }

    public static void ExportChart(string exportPath)
    {
        var dotChartLines = GenerateData();

        var name = Chart.Metadata.SongInfo[Metadata.MetadataType.name];
        var artist = Chart.Metadata.SongInfo[Metadata.MetadataType.artist];
        var charter = Chart.Metadata.SongInfo[Metadata.MetadataType.charter];
        var chartFolderPath = $"{exportPath}\\{artist} - {name} ({charter})";

        if (Directory.Exists(chartFolderPath))
        {
            // prompt user to overwrite or not!!!!

            Directory.Delete(exportPath);
        }
        string dotChartPath = $"{chartFolderPath}\\notes.chart";

        // template of files to make (needs more working, checking if we need them, etc.)
        Directory.CreateDirectory(chartFolderPath);
        File.Create(dotChartPath);
        File.Create($"{chartFolderPath}\\song.ini");
        File.Create($"{chartFolderPath}\\album.png"); // for this and the next, store image data as bytes and then write as bytes
        File.Create($"{chartFolderPath}\\background.png");

        File.WriteAllLines(dotChartPath, dotChartLines);
    }

    private static List<string> GenerateData()
    {
        List<string> dotChartLines = new();

        dotChartLines.AddRange(WriteSong());
        dotChartLines.AddRange(WriteSyncTrack());
        dotChartLines.AddRange(WriteGlobalEvents());

        foreach (var instrument in Chart.Instruments)
        {
            if (instrument == null) continue;
            dotChartLines.AddRange(WriteInstrument(instrument));
        }
        return dotChartLines;
    }

    private static List<string> WriteIni()
    {
        // no opening curly brace needed
        List<string> iniLines = new()
        {
            "[Song]"
        };

        foreach (var metadatum in Chart.Metadata.SongInfo)
        {
            iniLines.Add($"{metadatum.Key} = {metadatum.Value}");
        }
        foreach(var difficulty in Chart.Metadata.Difficulties)
        {
            iniLines.Add($"{difficulty.Key} = {difficulty.Value}");
        }
        return iniLines;
    }

    private static List<string> WriteSong()
    {
        // Chart file format specifications ordering
        // https://docs.google.com/document/d/1v2v0U-9HQ5qHeccpExDOLJ5CMPZZ3QytPmAG5WF0Kzs

        List<string> songGroup = WriteHeader(HeaderType.Song);

        // make not magic pls and thank you
        for (int i = CHART_FIELDS_ENUM_START_POINT; i <= CHART_FIELDS_ENUM_END_POINT; i++)
        {
            var metadataField = (Metadata.MetadataType)i;
            songGroup.Add($"\t{MiscTools.Capitalize(metadataField.ToString())} = \"{Chart.Metadata.SongInfo[metadataField]}\"");
        }

        songGroup.Add($"\t{OFFSET}");
        songGroup.Add($"\tResolution = {Chart.Resolution}");

        // Skip Player2 and Difficulty (add later if GH3 support is requested)
        var startTime = Chart.Metadata.SongInfo[Metadata.MetadataType.preview_start_time];
        songGroup.Add($"\tPreviewStart = {startTime}");
        songGroup.Add($"\tPreviewEnd = {int.Parse(startTime) + UserSettings.DefaultPreviewLength}");
        
        foreach (var stem in Chart.Metadata.StemPaths)
        {
            var enumAsString = stem.Key.ToString();
            var cleanedEnumString = MiscTools.Capitalize(enumAsString.Replace("_", ""));

            // specifications say drums stream should be DrumStream, but
            // StemType has it listed as Drums and Drums_x for other parts of the program (waveform selector, mixer)
            if (cleanedEnumString.Contains("Drums"))
            {
                cleanedEnumString = cleanedEnumString.Replace("s", "");
            }

            // should be MusicStream, not SongStream
            if (cleanedEnumString == "Song")
            {
                cleanedEnumString = "Music";
            }

            var streamString = cleanedEnumString + "Stream";

            var lastDirectoryPath = stem.Value.LastIndexOf("/");
            var trackName = stem.Value[(lastDirectoryPath+1)..];

            songGroup.Add($"\t{streamString} = \"{trackName}\"");
        }

        songGroup.Add(CLOSING_GROUP_CHAR);
        return songGroup;
    }

    private static List<string> WriteSyncTrack()
    {
        List<string> syncTrackEvents = WriteHeader(HeaderType.SyncTrack);
        var syncTrackStrings = Chart.SyncTrackInstrument.ExportAllEvents();

        syncTrackEvents.AddRange(syncTrackStrings);

        syncTrackEvents.Add(CLOSING_GROUP_CHAR);
        return syncTrackEvents;
    }

    private static List<string> WriteGlobalEvents()
    {
        List<string> globalEvents = WriteHeader(HeaderType.Events);

        // add events when implemented (sections, lyrics)

        globalEvents.Add(CLOSING_GROUP_CHAR);
        return globalEvents;
    }

    private static List<string> WriteInstrument(IInstrument instrument)
    {
        List<string> instrumentEvents = WriteHeader(GetMatchingHeader(instrument));

        instrumentEvents.AddRange(instrument.ExportAllEvents());
        instrumentEvents.Add(CLOSING_GROUP_CHAR);
        return instrumentEvents;
    }

    private static List<string> WriteHeader(HeaderType header)
    {
        List<string> lines = new(2)
        {
            $"[{header}]",
            "{"
        };
        return lines;
    }

    private static HeaderType GetMatchingHeader(IInstrument instrument)
    {
        int enumValue = (int)instrument.InstrumentName + (int)instrument.Difficulty;
        return (HeaderType)enumValue;
    }
}
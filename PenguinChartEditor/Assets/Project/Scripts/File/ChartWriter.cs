using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ChartWriter
{
    private const int CHART_FIELDS_ENUM_START_POINT = 0;
    private const int CHART_FIELDS_ENUM_END_POINT = 5;
    private const string CLOSING_GROUP_CHAR = "}";
    private const string OFFSET = "Offset = 0";

    public static void WriteChart(
        string targetDirectory, 
        int resolution,
        Metadata metadata,
        List<IInstrument> instruments, 
        AudioFormats audioFormat
        )
    {
        var filePath = $"{targetDirectory}/notes.chart";
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        File.Create(filePath);
        
        List<string> dotChartLines = new();
        
        dotChartLines.AddRange(WriteSong(metadata, resolution, audioFormat));
        dotChartLines.AddRange(WriteInstrument(instruments.First(x => x.InstrumentID == HeaderType.SyncTrack)));
        dotChartLines.AddRange(WriteGlobalEvents(instruments.First(x => x.InstrumentID == HeaderType.Events) as SectionInstrument));
        
        foreach (var instrument in instruments.Where(instrument => instrument != null))
        {
            dotChartLines.AddRange(WriteInstrument(instrument));
        }
        
        File.WriteAllLines(filePath, dotChartLines);
    }

    private static List<string> WriteSong(Metadata metadata, int resolution, AudioFormats audioFormat)
    {
        // Chart file format specifications ordering
        // https://docs.google.com/document/d/1v2v0U-9HQ5qHeccpExDOLJ5CMPZZ3QytPmAG5WF0Kzs

        List<string> songGroup = WriteHeader(HeaderType.Song);

        for (int i = CHART_FIELDS_ENUM_START_POINT; i <= CHART_FIELDS_ENUM_END_POINT; i++)
        {
            var metadataField = (Metadata.MetadataType)i;
            songGroup.Add($"\t{MiscTools.Capitalize(metadataField.ToString())} = \"{metadata.SongInfo[metadataField]}\"");
        }

        songGroup.Add($"\t{OFFSET}");
        songGroup.Add($"\tResolution = {resolution}");

        // Skip Player2 and Difficulty (add later if GH3 support is requested)

        int startTime = (int)Mathf.Round(metadata.PreviewStartTime * 1000);
        songGroup.Add($"\tPreviewStart = {startTime}");
        songGroup.Add($"\tPreviewEnd = {startTime + UserSettings.DefaultPreviewLength}");
        
        foreach (var stem in metadata.StemPaths)
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

            songGroup.Add($"\t{streamString} = \"{stem.Key}.{audioFormat}\"");
        } 

        songGroup.Add(CLOSING_GROUP_CHAR);
        return songGroup;
    }

    private static List<string> WriteGlobalEvents(SectionInstrument sections)
    {
        List<string> globalEvents = WriteHeader(HeaderType.Events);
        
        // add text lyrics when applicable
        globalEvents.AddRange(sections.ExportDotChartData());

        globalEvents.Add(CLOSING_GROUP_CHAR);
        return globalEvents;
    }

    private static List<string> WriteInstrument(IInstrument instrument)
    {
        List<string> instrumentEvents = WriteHeader(InstrumentMetadata.GetHeader(instrument));

        instrumentEvents.AddRange(instrument.ExportDotChartData());
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
}
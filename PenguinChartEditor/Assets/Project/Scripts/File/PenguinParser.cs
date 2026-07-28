using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class PenguinParser
{
    public static ChartFileInformation ParsePenguin(string filePath)
    {
        var fileData = new ChartFileInformation();

        Chart.loadFileState = "[DotPenguin]: Reading file data..."; 
        var fileAsLines = File.ReadAllLines(filePath);
        Chart.loadFileState = "[DotPenguin]: Dividing sections...";
        var baseSections = FormatEventSections(fileAsLines, indent: 0);

        Chart.loadFileState = "[DotPenguin]: Dividing sections (part 2)...";
        Dictionary<int, List<PenguinEventSection>> sectionsLevel1 = new();
        foreach (var section in baseSections)
        {
            var secondLevelSections = FormatEventSections(section.lines, indent: 1);
            sectionsLevel1[section.id] = secondLevelSections;
        }

        Chart.loadFileState = "[DotPenguin]: Making metadata...";
        fileData.metadata = new Metadata(sectionsLevel1[(int)HeaderType.Song]);

        Chart.loadFileState = "[DotPenguin]: Creating instruments...";
        ConcurrentBag<IInstrument> parsedInstruments = new();
        Parallel.ForEach
        (
            sectionsLevel1, 
            x =>
            {
                var instrument = CreateInstrument(x.Key, x.Value);
                if (instrument is null) return;
                
                switch (instrument.InstrumentID)
                {
                    case HeaderType.SyncTrack:
                        fileData.syncTrack = (SyncTrackInstrument)instrument;
                        return;
                    case HeaderType.Events:
                        fileData.sections = (SectionInstrument)instrument;
                        return;
                    case HeaderType.Starpower:
                        fileData.starpower = (StarpowerInstrument)instrument;
                        return;
                }

                parsedInstruments.Add(instrument);
            }
        );

        Chart.loadFileState = "[DotPenguin]: Returning to main (returning chart pak)...";
        fileData.traditionalInstruments = parsedInstruments.ToList();

        return fileData;
    }

    private static List<PenguinEventSection> FormatEventSections(string[] penguinLines, int indent = 0)
    {
        var curlyIndent = string.Concat(Enumerable.Repeat("\t", indent));
        var checkString = curlyIndent + "{";
        
        var sectionStartPoints = Enumerable.Range(0, penguinLines.Length).
            Where(
                i => penguinLines[i] == checkString
            ).ToList();

        var sections = new List<PenguinEventSection>();
        for (int i = 0; i < sectionStartPoints.Count; i++)
        {
            var openingCurlyIndex = sectionStartPoints[i];
            
            var id = int.Parse(penguinLines[openingCurlyIndex - 1]);

            int sectionEndPoint;
            if (i + 1 >= sectionStartPoints.Count)
            {
                sectionEndPoint = penguinLines.Length - 1;
            }
            else
            {
                // Structured as such:
                // id
                // {
                // ...
                // }        <----- end of section
                // id
                // {        <----- id + 1
                // so index(id) - 2 gives the end point of the previous section.
                sectionEndPoint = sectionStartPoints[i + 1] - 2;
            }
            
            // yields everything within { } but not including the braces
            sections.Add(new 
                PenguinEventSection(
                    penguinLines[(openingCurlyIndex+1)..sectionEndPoint], 
                    id
                    )
            );
        }

        return sections;
    }

    private static IInstrument CreateInstrument(int id, List<PenguinEventSection> lanes)
    {
        var instrumentID = (HeaderType)id;

        switch (instrumentID)
        {
            // Won't be parsed or already parsed.
            case HeaderType.Penguin or HeaderType.Song:
                return null;
            case HeaderType.SyncTrack:
                return new SyncTrackInstrument(lanes);
            case HeaderType.Starpower:
                return new StarpowerInstrument(lanes);
            case HeaderType.Events:
                return new SectionInstrument(lanes);
            default:
                switch (InstrumentMetadata.GetInstrumentGroup(instrumentID))
                {
                    case InstrumentCategory.FiveFret:
                        return new FiveFretInstrument(instrumentID, lanes);
                    default:
                        Debug.LogWarning($"No parsing logic for instrument {instrumentID}");
                        return null;
                }
        }
    }
}

public class PenguinEventSection
{
    public string[] lines;
    public int id;

    public PenguinEventSection(string[] lines, int id)
    {
        this.lines = lines;
        this.id = id;
    }
}
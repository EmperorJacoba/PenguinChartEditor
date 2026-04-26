using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;

public static class PenguinParser
{
    public static ChartFileInformation ParsePenguin(string filePath)
    {
        var fileData = new ChartFileInformation();
        
        var fileAsLines = File.ReadAllLines(filePath);
        var baseSections = FormatEventSections(fileAsLines, indent: 0);

        Dictionary<int, List<PenguinEventSection>> sectionsLevel1 = new();
        foreach (var section in baseSections)
        {
            var secondLevelSections = FormatEventSections(section.lines, indent: 1);
            sectionsLevel1[section.id] = secondLevelSections;
        }

        fileData.metadata = new Metadata(sectionsLevel1[(int)HeaderType.Song]);
        
        
        return null;
        // make metadata, then make an IInstrument out of each of the others in parallel
    }

    public static List<PenguinEventSection> FormatEventSections(string[] penguinLines, int indent = 0)
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
            if (i + 1 > sectionStartPoints.Count)
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
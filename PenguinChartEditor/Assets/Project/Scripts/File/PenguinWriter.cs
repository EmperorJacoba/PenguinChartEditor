using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class PenguinWriter
{
    public static void WritePenguin(
        string targetDirectory,
        Metadata metadata,
        List<IInstrument> instruments
        )
    {
        List<string> dotPenguinLines = new List<string>();
        
        dotPenguinLines.Add($"{HeaderType.Penguin}");
        dotPenguinLines.Add("{");
        dotPenguinLines.Add($"\t{Application.version}");
        dotPenguinLines.Add($"\t{DateTime.UtcNow} UTC-0");
        dotPenguinLines.Add("}");
        dotPenguinLines.AddRange(metadata.ToPenguinFormat());

        foreach (var instrument in instruments.Where(instrument => instrument != null))
        {
            dotPenguinLines.AddRange(instrument.ExportDotPenguinData());
        }
        
        var filePath = $"{targetDirectory}/{metadata.GeneratePenguinFileName()}.penguin";
        filePath = @"\\?\" + filePath;
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
        Debug.Log(filePath);
        File.WriteAllLines(filePath, dotPenguinLines);
    }
}
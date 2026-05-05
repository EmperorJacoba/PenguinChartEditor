using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class PenguinWriter
{
    private const float WRITER_VERSION = 1;
    public static void WritePenguin(
        string targetDirectory,
        Metadata metadata,
        List<IInstrument> instruments,
        string fileName
        )
    {
        if (metadata is null || instruments is null) return;
        List<string> dotPenguinLines = new List<string>();
        
        dotPenguinLines.Add($"{(int)HeaderType.Penguin}");
        dotPenguinLines.Add("{");
        dotPenguinLines.Add($"\t{Application.version}");
        dotPenguinLines.Add($"\t{DateTime.UtcNow} UTC-0");
        dotPenguinLines.Add($"\t{WRITER_VERSION}");
        dotPenguinLines.Add("}");
        dotPenguinLines.AddRange(metadata.ToPenguinFormat());

        foreach (var instrument in instruments.Where(instrument => instrument != null))
        {
            dotPenguinLines.AddRange(instrument.ExportDotPenguinData());
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }
        
        var filePath = $"{targetDirectory}/{fileName}.penguin";
        
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // Used to circumvent errors from file paths that are too long. Added because test charts have reeeeeally long names.
        filePath = @"\\?\" + filePath;
#endif
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
        File.WriteAllLines(filePath, dotPenguinLines);
        Debug.Log($"Saved to {filePath}");
    }
}
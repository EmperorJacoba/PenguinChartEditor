using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class IniWriter
{
    private static List<string> GenerateIniText(Metadata metadata)
    {
        // no opening curly brace needed
        List<string> iniLines = new()
        {
            "[Song]"
        };
        iniLines.AddRange(metadata.SongInfo.Select(metadatum => $"{metadatum.Key} = {metadatum.Value}"));
        iniLines.AddRange(metadata.Difficulties.Select(difficulty => $"{difficulty.Key} = {difficulty.Value}"));
        iniLines.Add($"song_length = {Mathf.CeilToInt(SongTime.SongLength * 1000)}");
        iniLines.Add($"preview_start_time = {Mathf.CeilToInt(metadata.PreviewStartTime * 1000)}");
        
        return iniLines;
    }

    public static void WriteIni(string targetDirectory, Metadata metadata)
    {
        var formattedFilePath = $"{targetDirectory}/song.ini";
        if (File.Exists(formattedFilePath))
        {
            File.Delete(formattedFilePath);
        }
        File.WriteAllLines(formattedFilePath, GenerateIniText(metadata));
    }
}
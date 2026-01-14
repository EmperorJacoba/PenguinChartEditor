using System.Collections.Generic;
using TextCopy;
using UnityEngine;

public static class Clipboard
{
    public static void Copy()
    {
        ClipboardService.SetText(Chart.LoadedInstrument.ConvertSelectionToString());
    }

    public static void Paste()
    {
        var userText = ClipboardService.GetText();
        var offset = Previewer.previewTick;

        Chart.LoadedInstrument.AddChartFormattedEventsToInstrument(userText, offset);
        Chart.InPlaceRefresh();
    }

    public static List<KeyValuePair<int, string>> ConvertToLineList(string clipboardData, int offset)
    {
        var lines = new List<KeyValuePair<int, string>>();
        var clipboardAsLines = clipboardData.Split("\n");

        for (int i = 0; i < clipboardAsLines.Length; i++)
        {
            var line = clipboardAsLines[i];

            if (line.Trim() == "") continue;
            var parts = line.Split(" = ", 2);

            if (!int.TryParse(parts[0].Trim(), out int tick))
            {
                Chart.Log(@$"Problem parsing event {line}");
                continue;
            }

            if (i == 0)
            {
                offset -= tick;
            }

            lines.Add(new(tick + offset, parts[1]));
        }

        return lines;
    }

    public static void Cut()
    {
        Copy();
        Chart.LoadedInstrument.DeleteTicksInSelection();
        Chart.InPlaceRefresh();
    }
}
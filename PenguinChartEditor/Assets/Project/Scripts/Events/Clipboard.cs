using UnityEngine;
using TextCopy;

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

    public static void Cut()
    {
        Copy();
        Chart.LoadedInstrument.DeleteTicksInSelection();
        Chart.InPlaceRefresh();
    }
}
using UnityEngine;
using TextCopy;

// this will need to have extended support for star power down the line
// star power could be its own instrument entirely????? instead of embedded within classes
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
        Chart.Refresh();
    }

    public static void Cut()
    {
        Copy();
        Chart.LoadedInstrument.DeleteTicksInSelection();
        Chart.Refresh();
    }
}
using UnityEngine;
using TextCopy;

// this will need to have extended support for star power down the line
// star power is its own instrument entirely????? instead of embedded within classes
public static class Clipboard
{
    public static void Copy()
    {
        ClipboardService.SetText(Chart.LoadedInstrument.ConvertSelectionToString());
    }

    public static void Paste()
    {
        // get loaded instrument
        // get clipboard text
        // attempt to convert data into event data
        // set events within instruments
    }
}
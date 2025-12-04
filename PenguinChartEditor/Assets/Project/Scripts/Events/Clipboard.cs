using UnityEngine;
using TextCopy;

// this will need to have extended support for star power down the line
// star power is its own instrument entirely????? instead of embedded within classes
public class Clipboard : MonoBehaviour
{
    public void Copy()
    {
        // get loaded instrument
        // convert instrument data to .chart format string
        // set clipboard text to that, separated by \n and \t


    }

    public void Paste()
    {
        // get loaded instrument
        // get clipboard text
        // attempt to convert data into event data
        // set events within instruments
    }
}
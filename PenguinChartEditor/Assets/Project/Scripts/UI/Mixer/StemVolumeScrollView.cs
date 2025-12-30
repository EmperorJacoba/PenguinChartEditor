using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The script attached to the scroll view GameObject that contains the volume sliders.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class StemVolumeScrollView : MonoBehaviour
{
    [SerializeField] GameObject volumeEditor;
    [SerializeField] RectTransform scrollViewContentRt;
    [SerializeField] ScrollRect scrollView;
    const int BUFFER = -20;

    void Start()
    {
        var volumeEditorHeight = volumeEditor.GetComponent<RectTransform>().sizeDelta.y;

        // Create volume slider for each stem AND fit a scroll view perfectly around them!
        // Set the height of the content rect transform so that it can fit all the volume sliders,
        // plus a little bit so that the last slider isn't cut off
        scrollViewContentRt.sizeDelta = new Vector2(0, volumeEditorHeight * Chart.Metadata.StemPaths.Count + (volumeEditorHeight / 2));

        int numEntries = 0;
        // Create a slider package for each stem the user has
        foreach (var entry in Chart.Metadata.StemPaths)
        {
            var currentEditor = Instantiate(volumeEditor, scrollViewContentRt.transform);
            // Update the text on the given slider package for the current stem
            currentEditor.GetComponent<StemVolumeEditor>().StemType = entry.Key;

            // Space the packages evenly from one another
            // Pivot of the content rect is at the top, so stem packages fill in downwards
            // Buffer is needed to prevent clipping with the top of the panel
            var yPos = BUFFER - (volumeEditorHeight / 2) - (numEntries * volumeEditorHeight);
            currentEditor.GetComponent<RectTransform>().localPosition = new Vector2(0, yPos);

            numEntries++;
        }

        scrollView.normalizedPosition = new Vector2(0, 1);
    }
}

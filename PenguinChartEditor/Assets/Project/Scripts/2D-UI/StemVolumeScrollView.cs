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

    void Start()
    {
        var volumeEditorHeight = volumeEditor.GetComponent<RectTransform>().sizeDelta.y;

        // Create volume slider for each stem AND fit a scroll view perfectly around them!
        // Set the height of the content rect transform so that it can fit all the volume sliders
        scrollViewContentRt.sizeDelta = new Vector2(0, volumeEditorHeight * Chart.Metadata.StemPaths.Count);

        int numEntries = 0;
        // Create a slider package for each stem the user has
        foreach (var entry in Chart.Metadata.StemPaths)
        {
            var currentEditor = Instantiate(volumeEditor, scrollViewContentRt.transform);
            // Update the text on the given slider package for the current stem
            currentEditor.GetComponent<StemVolumeEditor>().StemType = entry.Key;

            // Space the packages evenly from one another
            var yPos = (scrollViewContentRt.sizeDelta.y / 2) - (volumeEditorHeight / 2) - (numEntries * volumeEditorHeight);
            currentEditor.GetComponent<RectTransform>().localPosition = new Vector2(0, yPos);

            numEntries++;
        }

        // This sets the scroll view to the top
        // The rect is in the middle of the scroll view so that the math above works out cleanly, 
        // so this is needed to set the scroll view to the top (for cleanliness)
        scrollView.normalizedPosition = new Vector2(0, 1);
    }

}

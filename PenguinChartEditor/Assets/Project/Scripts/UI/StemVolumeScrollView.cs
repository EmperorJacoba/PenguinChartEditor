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

    void Start()
    {
        // Create a slider package for each stem the user has
        foreach (var entry in Chart.Metadata.StemPaths)
        {
            var currentEditor = Instantiate(volumeEditor, scrollViewContentRt.transform);

            // Update the text on the given slider package for the current stem
            currentEditor.GetComponent<StemVolumeEditor>().StemType = entry.Key;
        }
    }
}

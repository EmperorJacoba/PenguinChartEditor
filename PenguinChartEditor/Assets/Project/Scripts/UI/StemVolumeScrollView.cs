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
        // The UI object containing all stems is uses Content Size Fitter & Vertical Layout Group to automatically fit objects.
        foreach (var entry in Chart.Metadata.StemPaths)
        {
            var currentEditor = Instantiate(volumeEditor, scrollViewContentRt.transform);

            currentEditor.GetComponent<StemVolumeEditor>().StemType = entry.Key;
        }
    }
}

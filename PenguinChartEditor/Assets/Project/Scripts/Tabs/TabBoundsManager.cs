using UnityEngine;

public class TabBoundsManager : MonoBehaviour
{
    [SerializeField] private GameObject contentPanel;

    private void Start()
    {
        ResizeTabContent();
    }

    /// <summary>
    /// Fit the contents of the tab scene into the content area in ContainerScene.
    /// </summary>
    private void ResizeTabContent()
    {
        // Get the RT components of both the alloted area and content itself, both of which are stored as panels
        var displayedTabZoneRt = GameObject.Find("DisplayedTabZone").GetComponent<RectTransform>();
        var contentPanelRt = contentPanel.GetComponent<RectTransform>();

        // Match the anchors of the content to that of the alloted area for proper resizing
        contentPanelRt.anchoredPosition = displayedTabZoneRt.anchoredPosition;
        contentPanelRt.anchorMax = displayedTabZoneRt.anchorMax;
        contentPanelRt.anchorMin = displayedTabZoneRt.anchorMin;
    }
}

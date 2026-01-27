using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LeftOptionsPanelTab : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private LeftOptionsPanel leftOptionsPanel;
    [SerializeField] private LeftOptionsPanel.PanelType thisPanelTab;
    [SerializeField] private Image imageComponent;
    [SerializeField] private Sprite tabActiveImage;
    [SerializeField] private Sprite tabInactiveImage;

    public void OnPointerClick(PointerEventData data)
    {
        leftOptionsPanel.SwitchPanel(thisPanelTab);
    }

    public void SwitchToActive()
    {
        imageComponent.sprite = tabActiveImage;
    }

    public void SwitchToInactive()
    {
        imageComponent.sprite = tabInactiveImage;
    }
}
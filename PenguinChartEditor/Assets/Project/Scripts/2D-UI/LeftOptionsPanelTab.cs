using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LeftOptionsPanelTab : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] LeftOptionsPanel leftOptionsPanel;
    [SerializeField] LeftOptionsPanel.PanelType thisPanelTab;
    [SerializeField] Image imageComponent;
    [SerializeField] Sprite tabActiveImage;
    [SerializeField] Sprite tabInactiveImage;

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
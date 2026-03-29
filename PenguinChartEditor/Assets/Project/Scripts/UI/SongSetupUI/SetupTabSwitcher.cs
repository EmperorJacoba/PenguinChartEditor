using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SetupTabSwitcher : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private PenguinTabImages imageReference;
    private Image imageComponent;
    public GameObject controlledComponent;

    private static readonly List<SetupTabSwitcher> tabs = new();

    private void Awake()
    {
        imageComponent = GetComponent<Image>();
        tabs.Add(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (imageComponent.sprite == imageReference.tabActiveImage) return;
        
        controlledComponent.SetActive(true);
        foreach (var tab in tabs.Where(x => x != this))
        {
            tab.SwitchOff();
        }

        imageComponent.sprite = imageReference.tabActiveImage;
    }

    private void SwitchOff()
    {
        controlledComponent.SetActive(false);
        imageComponent.sprite = imageReference.tabInactiveImage;
    }
}
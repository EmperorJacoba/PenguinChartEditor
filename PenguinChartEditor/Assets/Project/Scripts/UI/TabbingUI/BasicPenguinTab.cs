using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface ITab
{
    void SwitchOn();
    void SwitchOff();
}

public abstract class BasicPenguinTab<T> : MonoBehaviour, IPointerDownHandler, ITab where T : ITab
{
    [SerializeField] private PenguinTabImages imageReference;
    [SerializeField] private Image imageComponent;

    protected static readonly List<ITab> tabs = new();
    protected static ITab loadedTab;

    private void Awake()
    {
        imageComponent ??= GetComponent<Image>();
        tabs.Add(this);
        
        if (imageComponent.sprite == imageReference.tabActiveImage)
        {
            loadedTab = this;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SwitchOn();
    }

    public void SwitchOn()
    {
        if (imageComponent.sprite == imageReference.tabActiveImage) return;
        
        loadedTab?.SwitchOff();
        OnSwitchOn();

        imageComponent.sprite = imageReference.tabActiveImage;

        loadedTab = this;
    }

    public void SwitchOff()
    {
        OnSwitchOff();
        imageComponent.sprite = imageReference.tabInactiveImage;

        loadedTab = null;
    }

    public static void SwitchOffActiveTab()
    {
        loadedTab?.SwitchOff();
    }

    protected abstract void OnSwitchOff();
    protected abstract void OnSwitchOn();

    private void OnDestroy()
    {
        tabs.Remove(this);
    }
}
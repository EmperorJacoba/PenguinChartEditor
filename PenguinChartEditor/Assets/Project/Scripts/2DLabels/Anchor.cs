using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Anchor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] Image lockImage;
    [SerializeField] BPMLabel parentBPM;
    const int MAX_ALPHA_CHANNEL_VALUE = 255;

    void Awake()
    {
        Opacity = 0;
    }

    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            gameObject.SetActive(value);
        }
    }

    public float Opacity
    {
        get
        {
            return lockImage.color.a;
        }
        set
        {
            lockImage.color = new(lockImage.color.r, lockImage.color.g, lockImage.color.b, value);
        }
    }

    bool isAnchor => Tempo.AnchoredEvents.Contains(parentBPM.Tick);

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if (!isAnchor) Opacity = 0.5f;
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if (!isAnchor) Opacity = 0f;
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if (isAnchor)
        {
            Tempo.AnchoredEvents.Remove(parentBPM.Tick);
            parentBPM.RefreshEvents();
        }
        else
        {
            Tempo.AnchoredEvents.Add(parentBPM.Tick);
            parentBPM.RefreshEvents();
        }
    }
}

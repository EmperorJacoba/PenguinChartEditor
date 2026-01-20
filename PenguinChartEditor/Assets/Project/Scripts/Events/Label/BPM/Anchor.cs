using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Anchor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image lockImage;
    [SerializeField] private BPMLabel parentBPM;

    private void Awake()
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

    private bool IsAnchor => Chart.SyncTrackInstrument.TempoEvents[parentBPM.Tick].Anchor;

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if (!IsAnchor) Opacity = 0.5f;
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if (!IsAnchor) Opacity = 0f;
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        Chart.SyncTrackInstrument.TempoEvents[parentBPM.Tick] = new BPMData(Chart.SyncTrackInstrument.TempoEvents[parentBPM.Tick].BPMChange, Chart.SyncTrackInstrument.TempoEvents[parentBPM.Tick].Timestamp, !IsAnchor);
        Chart.InPlaceRefresh();
    }
}

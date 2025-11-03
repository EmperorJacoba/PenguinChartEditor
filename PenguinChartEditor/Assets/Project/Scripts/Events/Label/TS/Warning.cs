using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Warning : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPoolable
{
    [SerializeField] RectTransform rectTransform;
    public void OnPointerEnter(PointerEventData eventData) => Tooltip.NewTooltip(warningDescriptions[type]);
    public void OnPointerExit(PointerEventData eventData) => Tooltip.Deactivate();
    public Coroutine destructionCoroutine { get; set; }

    public void InitializeWarning(WarningType warningType)
    {
        type = warningType;

        gameObject.SetActive(true);
    }

    public int Tick => _tick;
    int _tick;

    public void InitializeEvent(int tick, float highwayLength)
    {
        _tick = tick;
        Visible = true;
        UpdatePosition(Waveform.GetWaveformRatio(tick), highwayLength);
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

    WarningType type;
    public enum WarningType
    {
        invalidTimeSignature
    }

    public static readonly Dictionary<WarningType, string> warningDescriptions = new()
    {
        {
            WarningType.invalidTimeSignature,
            "This time signature change occurs at an invalid timestamp. Please place all time signature change events on the first beat of a bar."
        }
    };

    public void UpdatePosition(double percentOfScreen, float screenHeight)
    {
        var yScreenProportion = (float)percentOfScreen * screenHeight;
        transform.localPosition = new Vector3(transform.localPosition.x, yScreenProportion - (rectTransform.rect.height / 2));
    }
}
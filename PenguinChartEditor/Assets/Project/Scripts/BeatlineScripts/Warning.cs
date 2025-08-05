using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Warning : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData) => Tooltip.NewTooltip(warningDescriptions[type]);
    public void OnPointerExit(PointerEventData eventData) => Tooltip.Deactivate();

    public void InitializeWarning(WarningType warningType)
    {
        type = warningType;

        gameObject.SetActive(true);
    }

    public void DeactivateWarning()
    {
        gameObject.SetActive(false);
    }

    public bool Active
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
}
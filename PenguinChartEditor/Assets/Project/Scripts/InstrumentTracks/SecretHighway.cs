using UnityEngine;
using UnityEngine.EventSystems;

public class SecretHighway : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle) return; // middle click scroll will clear selections otherwise
        Chart.LoadedInstrument.ClearAllSelections();
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

public class Highway : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Chart.LoadedInstrument.ClearAllSelections();
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class ChartingHighway : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Chart.LoadedInstrument.ClearAllSelections();
    }
}

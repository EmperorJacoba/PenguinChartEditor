using UnityEngine;
using UnityEngine.EventSystems;

public class InstrumentAddButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] bool directionIsRight;
    public void OnPointerDown(PointerEventData eventData)
    {
        InstrumentAddBox.instance.Activate(directionIsRight);
    }
}
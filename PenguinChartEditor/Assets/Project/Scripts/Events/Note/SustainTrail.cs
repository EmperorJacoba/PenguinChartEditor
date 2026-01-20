using UnityEngine;
using UnityEngine.EventSystems;

public class SustainTrail : MonoBehaviour, IPointerDownHandler
{
    private IEvent parentNote;
    private ISustainableInstrument parentInstrument => parentNote.ParentInstrument as ISustainableInstrument;

    private void Awake()
    {
        parentNote = GetComponentInParent<IEvent>();
    }

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        parentInstrument.ChangeSustainFromTrail(pointerEventData, parentNote);
    }
}

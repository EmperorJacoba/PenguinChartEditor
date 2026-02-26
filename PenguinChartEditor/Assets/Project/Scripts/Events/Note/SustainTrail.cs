using UnityEngine;
using UnityEngine.EventSystems;

public class SustainTrail : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private IEvent parentNote;
    private ISustainableInstrument parentInstrument => parentNote.ParentInstrument as ISustainableInstrument;

    private void Awake()
    {
        parentNote = GetComponentInParent<IEvent>();
    }

    private bool firstFrame = true;
    public void OnPointerDown(PointerEventData pointerEventData)
    {
        if (pointerEventData.button != PointerEventData.InputButton.Right) return;
        
        parentInstrument.ChangeSustainFromTrail(pointerEventData, parentNote, firstFrame);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        
        firstFrame = true;
        parentInstrument.CompleteOpenSingleSustainUndoAction(parentNote);
    }
}

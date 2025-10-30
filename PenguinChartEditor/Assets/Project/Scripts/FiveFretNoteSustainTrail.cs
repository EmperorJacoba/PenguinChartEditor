using UnityEngine;
using UnityEngine.EventSystems;

public class FiveFretNoteSustainTrail : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] FiveFretNote parentNote;

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        FiveFretNote.resetSustains = true;
        if (pointerEventData.button == PointerEventData.InputButton.Right) parentNote.RemoveFromSelection();
    }

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        FiveFretNote.resetSustains = false;
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            var sustainClamp = parentNote.GetCurrentMouseTick() - parentNote.Tick;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                parentNote.chartInstrument.ShiftClickSelect(parentNote.Tick);
                parentNote.chartInstrument.ShiftClickSustainClamp(parentNote.Tick, sustainClamp);
            }
            parentNote.AddToSelection();
            parentNote.ClampSustain(sustainClamp);
        }
    }
}

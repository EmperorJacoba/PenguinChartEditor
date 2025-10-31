using UnityEngine;
using UnityEngine.EventSystems;

public class FiveFretNoteSustainTrail : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] FiveFretNote parentNote;

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        FiveFretNote.resetSustains = true;
        if (pointerEventData.button == PointerEventData.InputButton.Right && 
            parentNote.chartInstrument.Lanes.TempSustainTicks.Contains(parentNote.Tick))
        {
            parentNote.parentInstrument.RemoveTickFromAllSelections(parentNote.Tick);
            parentNote.RemoveFromSelection();
        }
        parentNote.chartInstrument.Lanes.TempSustainTicks.Clear();
    }

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        FiveFretNote.resetSustains = false;
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            var sustainClamp = parentNote.GetCurrentMouseTick() - parentNote.Tick;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                parentNote.chartInstrument.ShiftClickSelect(parentNote.Tick, true);
                parentNote.chartInstrument.ShiftClickSustainClamp(parentNote.Tick, sustainClamp);
            }
            parentNote.chartInstrument.Lanes.TempSustainTicks.Add(parentNote.Tick);
            parentNote.AddToSelection();
            parentNote.ClampSustain(sustainClamp);
        }
    }
}

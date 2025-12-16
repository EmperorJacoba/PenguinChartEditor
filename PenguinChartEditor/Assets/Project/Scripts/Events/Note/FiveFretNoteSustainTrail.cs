using UnityEngine;
using UnityEngine.EventSystems;

public class FiveFretNoteSustainTrail : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] FiveFretNote parentNote;

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        FiveFretInstrument.resetSustains = true;
        if (pointerEventData.button == PointerEventData.InputButton.Right && 
            parentNote.chartInstrument.Lanes.TempSustainTicks.Contains(parentNote.Tick))
        {
            parentNote.ParentInstrument.RemoveTickFromAllSelections(parentNote.Tick);
            parentNote.RemoveFromSelection();
        }
        parentNote.chartInstrument.Lanes.TempSustainTicks.Clear();
    }

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        FiveFretInstrument.resetSustains = false;
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            var sustainClamp = FiveFretInstrument.GetCurrentMouseTick() - parentNote.Tick;
            if (Input.GetKey(KeyCode.LeftShift) || !UserSettings.ExtSustains)
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

using UnityEngine;
using UnityEngine.EventSystems;

public class FiveFretNoteSustainTrail : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] FiveFretNote parentNote;

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        FiveFretInstrument.resetSustains = true;
    }

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        FiveFretInstrument.resetSustains = false;
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            parentNote.chartInstrument.ClearAllSelections();
            var sustainClamp = FiveFretInstrument.GetCurrentMouseTick() - parentNote.Tick;
            if (Input.GetKey(KeyCode.LeftShift) || !UserSettings.ExtSustains)
            {
                parentNote.chartInstrument.ShiftClickSelect(parentNote.Tick);
                parentNote.chartInstrument.ShiftClickSustainClamp(parentNote.Tick, sustainClamp);
            }
            parentNote.AddToSelection();
            parentNote.ClampSustain(sustainClamp);
            Chart.Refresh();
        }
    }
}

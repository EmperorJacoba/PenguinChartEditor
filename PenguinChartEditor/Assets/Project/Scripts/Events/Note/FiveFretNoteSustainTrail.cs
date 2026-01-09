using UnityEngine;
using UnityEngine.EventSystems;

public class FiveFretNoteSustainTrail : MonoBehaviour, IPointerDownHandler
{ 
    [SerializeField] FiveFretNote parentNote;

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            parentNote.ParentInstrument.ClearAllSelections();
            var sustainClamp = FiveFretInstrument.GetCurrentMouseTick() - parentNote.Tick;
            if (Input.GetKey(KeyCode.LeftShift) || !UserSettings.ExtSustains)
            {
                parentNote.ParentInstrument.ShiftClickSelect(parentNote.Tick);
                parentNote.ParentFiveFretInstrument.ShiftClickSustainClamp(parentNote.Tick, sustainClamp);
            }
            parentNote.AddToSelection();
            parentNote.ClampSustain(sustainClamp);
            Chart.InPlaceRefresh();
        }
    }
}

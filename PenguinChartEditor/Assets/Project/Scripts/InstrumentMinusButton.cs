using UnityEngine;
using UnityEngine.EventSystems;

public class InstrumentMinusButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] GameInstrumentIconLabel parentIconLabel;
    public void OnPointerClick(PointerEventData eventData)
    {
        InstrumentSpawningManager.instance.RemoveInstrument(parentIconLabel.parentGameInstrument);
    }
}
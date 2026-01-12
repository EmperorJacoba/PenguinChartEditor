using UnityEngine;
using UnityEngine.EventSystems;

public class InstrumentMoveButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] GameInstrumentIconLabel parentIconLabel;
    [SerializeField] bool isRight;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isRight)
        {
            InstrumentSpawningManager.instance.SwapInstrumentWithRight(parentIconLabel.parentGameInstrument);
        }
        else
        {
            InstrumentSpawningManager.instance.SwapInstrumentWithLeft(parentIconLabel.parentGameInstrument);
        }
    }
}
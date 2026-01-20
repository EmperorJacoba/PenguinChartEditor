using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The left and right arrows at the end of a track in multi-track (starpower) mode.
/// </summary>
public class InstrumentMoveButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameInstrumentIconLabel parentIconLabel;
    [SerializeField] private bool isRight;

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
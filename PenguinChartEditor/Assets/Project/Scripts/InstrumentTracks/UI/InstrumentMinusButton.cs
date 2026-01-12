using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The "remove" function at the end of a track in multi-track (starpower) mode.
/// </summary>
public class InstrumentMinusButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] GameInstrumentIconLabel parentIconLabel;
    public void OnPointerClick(PointerEventData eventData)
    {
        InstrumentSpawningManager.instance.RemoveInstrument(parentIconLabel.parentGameInstrument);
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Physical 3D button at the end of visible track set in multi-track (starpower) scene.
/// </summary>
public class InstrumentAddButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] bool directionIsRight;
    public void OnPointerDown(PointerEventData eventData)
    {
        InstrumentAddBox.instance.Activate(directionIsRight);
    }
}
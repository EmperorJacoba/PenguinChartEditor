using UnityEngine;
using UnityEngine.EventSystems;

// Tooltip system modeled after this video: https://youtu.be/HXFoUGw7eKk
// Thanks Game Dev Guide! (even though this technically isn't a game)
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] string displayInformation; 
    public void OnPointerEnter(PointerEventData eventData) => Tooltip.NewTooltip(displayInformation);
    public void OnPointerExit(PointerEventData eventData) => Tooltip.Deactivate();

}
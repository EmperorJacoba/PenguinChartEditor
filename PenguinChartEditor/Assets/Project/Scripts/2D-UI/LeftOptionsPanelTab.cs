using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeftOptionsPanelTab : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData data)
    {
        Debug.Log("Fired");
    }
}
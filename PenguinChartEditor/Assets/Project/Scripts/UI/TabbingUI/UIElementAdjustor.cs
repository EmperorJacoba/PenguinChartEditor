using System;
using UnityEngine;

public class UIElementAdjustor : MonoBehaviour
{
    private Vector2 targetCoordinates;
    private RectTransform rt;
    private void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        targetCoordinates = rt.anchoredPosition;
        
        PenguinSceneTabScaler.ScreenSizeUpdated += MoveElement;
    }

    private void OnDisable()
    {
        PenguinSceneTabScaler.ScreenSizeUpdated -= MoveElement;
    }

    private void MoveElement(float newCenterDelta)
    {
        rt.anchoredPosition = new Vector2(targetCoordinates.x, targetCoordinates.y - newCenterDelta);
    }
}
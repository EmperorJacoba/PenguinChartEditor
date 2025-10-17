using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public static class MiscTools
{
    public static string Capitalize(string name)
    {
        return char.ToUpper(name[0]) + name.Substring(1);
    }

    public static string Decapitalize(string name)
    {
        return char.ToLower(name[0]) + name.Substring(1);
    }

    public static bool IsRaycasterHit(BaseRaycaster targetRaycaster)
    {
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        targetRaycaster.Raycast(pointerData, results);

        // If a component from the toolboxes is raycasted from the cursor, then the overlay is hit.
        if (results.Count > 0) return true; else return false;
    }
}

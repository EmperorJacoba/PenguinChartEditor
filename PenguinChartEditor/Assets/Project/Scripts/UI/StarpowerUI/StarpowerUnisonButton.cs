using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StarpowerUnisonButton : MonoBehaviour
{
    private Button attachedButton;

    private void Awake()
    {
        attachedButton = GetComponent<Button>();
        attachedButton.onClick.AddListener(ApplyUnison);
    }

    private static void ApplyUnison()
    {
        Chart.StarpowerInstrument.MakeSelectionUnison();
        Chart.InPlaceRefresh();
    }
}
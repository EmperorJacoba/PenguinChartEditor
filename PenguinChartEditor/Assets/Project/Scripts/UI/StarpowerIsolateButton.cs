using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StarpowerIsolateButton : MonoBehaviour
{ 
    private Button attachedButton;

    private void Awake()
    {
        attachedButton = GetComponent<Button>();
        attachedButton.onClick.AddListener(IsolateStarpowerSelection);
    }

    private static void IsolateStarpowerSelection()
    {
        Chart.StarpowerInstrument.IsolateSelection();
        Chart.InPlaceRefresh();
    }
}
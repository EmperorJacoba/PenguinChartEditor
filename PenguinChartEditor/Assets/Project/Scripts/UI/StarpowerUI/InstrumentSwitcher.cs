using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InstrumentSwitcher : MonoBehaviour
{
    private Button button;
    [SerializeField] private bool isStarpowerSwitcher;

    private void Start()
    {
        button = GetComponent<Button>();
        
        button.onClick.AddListener(ChangeLoadedInstrument);
    }

    private void ChangeLoadedInstrument()
    {
        Chart.LoadedInstrument = isStarpowerSwitcher ? Chart.StarpowerInstrument : Chart.SectionInstrument;
        button.interactable = false;
        Chart.InPlaceRefresh();
    }

    private void Update()
    {
        button.interactable = isStarpowerSwitcher != (Chart.LoadedInstrument == Chart.StarpowerInstrument);
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

public class DisableOnInvalidInstrument : MonoBehaviour
{
    [SerializeField] private List<GameObject> enableForStarpower;
    [SerializeField] private List<GameObject> enableForSections;

    private IInstrument lastUpdatedInstrument;
    private void Update()
    {
        if (lastUpdatedInstrument == Chart.LoadedInstrument) return;
        
        foreach (var consideredObject in enableForSections)
        {
            consideredObject.SetActive(Chart.LoadedInstrument == Chart.SectionInstrument);
        }

        foreach (var consideredObject in enableForStarpower)
        {
            consideredObject.SetActive(Chart.LoadedInstrument == Chart.StarpowerInstrument);
        }

        lastUpdatedInstrument = Chart.LoadedInstrument;
    }
}
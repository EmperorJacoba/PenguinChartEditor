using System;
using UnityEngine;

public class LoadedInstrumentInjector : MonoBehaviour
{
    [SerializeField] private HeaderType sceneInstrument;

    private void Awake()
    {
        Chart.SetLoadedInstrument(sceneInstrument);
        Destroy(gameObject);
    }
}
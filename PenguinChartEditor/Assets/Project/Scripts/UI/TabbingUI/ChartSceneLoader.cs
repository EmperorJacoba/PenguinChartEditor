using System;
using UnityEngine;

public class ChartSceneLoader : MonoBehaviour
{
    [SerializeField] private GameInstrument targetGameInstrument;
    
    private static HeaderType requestedInstrument;
    
    public static void PrepareSceneLoad(HeaderType instrumentID)
    {
        requestedInstrument = instrumentID;
    }

    private void Awake()
    {
        if (TabSceneSpawningManager.IsTabbingActive())
        {
            targetGameInstrument.AssignInstrument(requestedInstrument);
            Chart.SetLoadedInstrument(requestedInstrument);
        }
        Destroy(gameObject);
    }
}
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TabSceneSpawningManager : MonoBehaviour
{
    // When tabbing scene is active, the canvas resizer must be overriden to conform to the ribbon. When testing/debugging,
    // it is much easier to run scenes standalone. 
    public static bool containerSceneIsActive { get; private set; }
    private static TabSceneSpawningManager instance;
    
    private void Awake()
    {
        instance = this;
        containerSceneIsActive = true;
    }

    private void Start()
    {
        SceneManager.LoadScene("FiveFretChartingScene", LoadSceneMode.Additive);
    }
}
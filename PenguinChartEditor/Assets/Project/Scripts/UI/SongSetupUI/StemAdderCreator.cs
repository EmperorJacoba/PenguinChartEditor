using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StemAdderCreator : MonoBehaviour
{
    // Pull up dropdown with available stems and then load a StemSourceSelector with that assigned stem
    // Load currently loaded/assigned audio stems on boot up
    // Detect audio stems from specified path (use extra object) - use mml and traditional naming standards to detect
    // Detect (from chart path) / (from other folder)

    [SerializeField] private Button addButton;
    [SerializeField] private TMP_Dropdown templateDropdown;
    private List<StemSourceSelector> activeStemSelectors;
    
    
    private void Awake()
    {
    }
}
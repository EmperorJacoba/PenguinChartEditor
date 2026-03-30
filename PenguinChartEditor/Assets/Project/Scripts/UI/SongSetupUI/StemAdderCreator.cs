using System;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private GameObject stemSourceSelectorPrefab;
    [SerializeField] private GameObject stemSpawningParent;
    [SerializeField] private GameObject canvas;
    private List<StemSourceSelector> activeStemSelectors = new();
    
    private void OnEnable()
    {
        addButton.onClick.AddListener(SpawnAudioMenu);

        foreach (var stem in Chart.Metadata.StemPaths)
        {
            SpawnStemSourceSelector(stem.Key);
        }
    }

    private void OnDisable()
    {
        foreach (var selector in activeStemSelectors)
        {
            Destroy(selector);
        }
        
        addButton.onClick.RemoveListener(SpawnAudioMenu);
        activeStemSelectors.Clear();
    }

    private List<StemType> optionIndeces;
    private TMP_Dropdown activeDropdown;
    
    private void SpawnAudioMenu()
    {
        activeDropdown = Instantiate(templateDropdown, canvas.transform);
        activeDropdown.transform.position = Input.mousePosition;
        activeDropdown.options.Clear();
        activeDropdown.options.Add(new TMP_Dropdown.OptionData("[Select]"));

        optionIndeces = new List<StemType> {0};
        foreach (var stemOption in Enum.GetValues(typeof(StemType)))
        {
            var enumStemOption = (StemType)stemOption;
            if (activeStemSelectors.Any(x => x.audioStemType == enumStemOption))
                continue;
            
            activeDropdown.options.Add(
                new TMP_Dropdown.OptionData(
                    MiscTools.Capitalize(enumStemOption.ToString().Replace("_", " "))
                    )
                );
            optionIndeces.Add(enumStemOption);
        }
        
        activeDropdown.onValueChanged.AddListener(ProcessAudioSelection);
    }

    private void ProcessAudioSelection(int option) => SpawnStemSourceSelector(optionIndeces[option]);

    private void SpawnStemSourceSelector(StemType stem)
    {
        if (activeDropdown is not null) Destroy(activeDropdown.gameObject);
        
        var stemSourceSelector = Instantiate(stemSourceSelectorPrefab, stemSpawningParent.transform).GetComponent<StemSourceSelector>();
        stemSourceSelector.Initialize(stem);
        activeStemSelectors.Add(stemSourceSelector);
    }
}
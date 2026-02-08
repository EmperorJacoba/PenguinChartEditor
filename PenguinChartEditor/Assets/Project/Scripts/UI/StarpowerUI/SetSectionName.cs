using System;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class SetSectionName : MonoBehaviour
{
    private TMP_InputField self;

    private void Awake()
    {
        self = GetComponent<TMP_InputField>();
        
        self.onValueChanged.AddListener(ChangeSectionName);
    }

    private void Update()
    {
        self.text = Chart.SectionInstrument.GetSelectedSectionName();
    }

    private static void ChangeSectionName(string input)
    {
        Chart.SectionInstrument.SetSectionSelectionName(input);
    }
}
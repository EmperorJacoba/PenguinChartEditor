using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class SectionDefaultNameSetter : MonoBehaviour
{
    private TMP_InputField self;

    private void Awake()
    {
        self = GetComponent<TMP_InputField>();
     
        self.text = SectionPreviewer.defaultSectionName;
        self.onValueChanged.AddListener(ChangeDefaultSectionName);
    }

    private static void ChangeDefaultSectionName(string input)
    {
        SectionPreviewer.defaultSectionName = input;
    }
}
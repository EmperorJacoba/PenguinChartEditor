using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class PenguinInputField : MonoBehaviour
{
    // FIXME: make this O(1) by having the input fields modify a static variable that reflects focus state
    public static bool IsInputFieldActive()
    {
        return knownInputFields.Any(inputField => inputField.isFocused);
    }
    
    private static List<TMP_InputField> knownInputFields = new();
    
    private TMP_InputField self;
    private void Awake()
    {
        self = GetComponent<TMP_InputField>();
        knownInputFields.Add(self);
    }

    private void OnDestroy()
    {
        knownInputFields.Remove(self);
    }
}
using UnityEngine;
using TMPro;
using System.Linq;

public class CurrentTimeEntryBoxValidator : MonoBehaviour
{
    [SerializeField] private TMP_InputField entryBox;

    // Code adapted from https://docs.unity3d.com/2018.3/Documentation/ScriptReference/UI.InputField-onValidateInput.html 
    private void Start()
    {
        entryBox.onValidateInput += delegate(string input, int charIndex, char addedChar) { return Validate(addedChar); };    
    }

    private char Validate(char charToValidate)
    {
        if (!allowedTimeCharacters.Contains(charToValidate))
        {
            charToValidate = '\0';
        }
        else if (entryBox.text.Contains(':') && charToValidate == ':')
        {
            charToValidate = '\0';
        }
        else if (entryBox.text.Contains('.') && charToValidate == '.')
        {
            charToValidate = '\0';
        }
        return charToValidate;
    }

    private readonly char[] allowedTimeCharacters = {'1', '2', '3', '4', '5', '6', '7', '8', '9', '0', ':', '.'};
}
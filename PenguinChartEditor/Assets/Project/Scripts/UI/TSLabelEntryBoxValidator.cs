using System.Linq;
using TMPro;
using UnityEngine;

public class TSLabelEntryBoxValidator : MonoBehaviour
{
    [SerializeField] TMP_InputField entryBox;

    // Code adapted from https://docs.unity3d.com/2018.3/Documentation/ScriptReference/UI.InputField-onValidateInput.html 
    void Start()
    {
        entryBox.onValidateInput += delegate(string input, int charIndex, char addedChar) { return Validate(addedChar); };    
    }

    char Validate(char charToValidate)
    {
        if (!allowedTSCharacters.Contains(charToValidate))
        {
            charToValidate = '\0';
        }
        else if (entryBox.text.Contains('/') && charToValidate == '/')
        {
            charToValidate = '\0';
        }
        return charToValidate;
    }

    char[] allowedTSCharacters = {'1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '/'};
}

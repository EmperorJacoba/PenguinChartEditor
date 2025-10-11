using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DivisionChanger : MonoBehaviour
{
    [SerializeField] TMP_InputField entryBox;
    InputMap inputMap;
    public static int CurrentDivision {get; set;} = 8;
    
    readonly int[] steps = {1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256, 384, 512, 768};

    void Start()
    {
        entryBox.text = CurrentDivision.ToString();
        entryBox.onValueChanged.AddListener(x => ManualDivisionChange(x));

        inputMap = new();
        inputMap.Enable();

        inputMap.ExternalCharting.IncreaseStep.performed += _ => { if (!Keyboard.current.ctrlKey.isPressed) IncreaseDivision(); };
        inputMap.ExternalCharting.DecreaseStep.performed += _ => { if (!Keyboard.current.ctrlKey.isPressed) DecreaseDivision(); };
        inputMap.ExternalCharting.IncreaseStepByOne.performed += _ => IncreaseDivisionByOne();
        inputMap.ExternalCharting.DecreaseStepByOne.performed += _ => DecreaseDivisionByOne();
    }
    public void IncreaseDivision()
    {
        if (CurrentDivision >= 768) return;
        if (!steps.Contains(CurrentDivision))
        {
            CurrentDivision = steps[~Array.BinarySearch(steps, CurrentDivision)];
        }
        else
        {
            CurrentDivision = steps[Array.IndexOf(steps, CurrentDivision) + 1];
        }


        entryBox.text = CurrentDivision.ToString();
    }

    public void DecreaseDivision()
    {
        if (CurrentDivision <= 1) return;

        if (!steps.Contains(CurrentDivision))
        {
            CurrentDivision = steps[~Array.BinarySearch(steps, CurrentDivision) - 1];
        }
        else
        {
            CurrentDivision = steps[Array.IndexOf(steps, CurrentDivision) - 1];
        }

        entryBox.text = CurrentDivision.ToString();
    }

    public void DecreaseDivisionByOne()
    {
        if (CurrentDivision <= 1) return;
        CurrentDivision--;
        entryBox.text = CurrentDivision.ToString();
    }

    public void IncreaseDivisionByOne()
    {
        if (CurrentDivision >= 768) return;
        CurrentDivision++;
        entryBox.text = CurrentDivision.ToString();
    }

    public void ManualDivisionChange(string manuallyEnteredValue)
    {
        CurrentDivision = int.Parse(manuallyEnteredValue);
    }
}

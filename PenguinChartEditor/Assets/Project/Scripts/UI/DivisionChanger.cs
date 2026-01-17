using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DivisionChanger : MonoBehaviour
{
    private const int MAX_DIVISION = 768;
    private const int MIN_DIVISION = 1;

    [SerializeField] TMP_InputField entryBox;
    [SerializeField] Button upButton;
    [SerializeField] Button downButton;

    InputMap inputMap;
    public static int CurrentDivision { get; set; } = 16;

    readonly int[] steps = { 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256, 384, 512, 768 };

    void Start()
    {
        upButton.onClick.AddListener(IncreaseDivision);
        downButton.onClick.AddListener(DecreaseDivision);

        entryBox.text = CurrentDivision.ToString();
        entryBox.onValueChanged.AddListener(x => ManualDivisionChange(x));

        inputMap = new();
        inputMap.Enable();

        inputMap.ExternalCharting.IncreaseStep.performed += _ => IncreaseDivision();
        inputMap.ExternalCharting.DecreaseStep.performed += _ => DecreaseDivision();
        inputMap.ExternalCharting.IncreaseStepByOne.performed += _ => IncreaseDivisionByOne();
        inputMap.ExternalCharting.DecreaseStepByOne.performed += _ => DecreaseDivisionByOne();
    }
    public void IncreaseDivision()
    {
        if (CurrentDivision >= MAX_DIVISION) return;
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
        if (CurrentDivision <= MIN_DIVISION) return;

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
        if (CurrentDivision <= MIN_DIVISION) return;
        CurrentDivision--;
        entryBox.text = CurrentDivision.ToString();
    }

    public void IncreaseDivisionByOne()
    {
        if (CurrentDivision >= MAX_DIVISION) return;
        CurrentDivision++;
        entryBox.text = CurrentDivision.ToString();
    }

    public void ManualDivisionChange(string manuallyEnteredValue)
    {
        CurrentDivision = int.Parse(manuallyEnteredValue);
    }
}

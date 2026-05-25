using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DivisionChanger : MonoBehaviour
{
    private const int MAX_DIVISION = 768;
    private const int MIN_DIVISION = 1;

    [SerializeField] private TMP_InputField entryBox;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;

    private InputMap inputMap;
    public static int CurrentDivision { get; set; } = 16;

    private readonly int[] steps = { 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256, 384, 512, 768 };

    private void Start()
    {
        upButton.onClick.AddListener(IncreaseDivision);
        downButton.onClick.AddListener(DecreaseDivision);

        entryBox.text = CurrentDivision.ToString();
        entryBox.onValueChanged.AddListener(x => ManualDivisionChange(x));

        inputMap = new InputMap();
        inputMap.Enable();

        inputMap.PenguinChartingUIShortcuts.IncreaseStep.performed += _ => IncreaseDivision();
        inputMap.PenguinChartingUIShortcuts.DecreaseStep.performed += _ => DecreaseDivision();
        inputMap.PenguinChartingUIShortcuts.IncreaseStepByOne.performed += _ => IncreaseDivisionByOne();
        inputMap.PenguinChartingUIShortcuts.DecreaseStepByOne.performed += _ => DecreaseDivisionByOne();
    }

    private void OnDestroy()
    {
        inputMap.Disable();
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

using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class DivisionChanger : MonoBehaviour
{
    [SerializeField] TMP_InputField entryBox;
    public static int CurrentDivision {get; set;} = 8;
    
    readonly int[] steps = {1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256, 384, 512, 768};

    void Start()
    {
        entryBox.text = CurrentDivision.ToString();
        entryBox.onValueChanged.AddListener(x => ManualDivisionChange(x));
    }
    public void IncreaseDivision()
    {
        RoundDivision();
        try
        {
            CurrentDivision = steps[Array.IndexOf(steps, CurrentDivision) + 1];
        }
        catch
        {
            CurrentDivision = 768;
        }
        entryBox.text = CurrentDivision.ToString();
    }

    public void DecreaseDivision()
    {
        RoundDivision();
        try
        {
            CurrentDivision = steps[Array.IndexOf(steps, CurrentDivision) - 1];
        }
        catch
        {
            CurrentDivision = 1;
        }
        entryBox.text = CurrentDivision.ToString();
    }

    void RoundDivision()
    {
        if (!steps.Contains(CurrentDivision))
        {
            var index = Array.BinarySearch(steps, CurrentDivision);
            index = ~index - 1;
            CurrentDivision = Math.Min(Math.Max(index, 0), steps.Length - 2);
        }
    }

    public void ManualDivisionChange(string manuallyEnteredValue)
    {
        CurrentDivision = int.Parse(manuallyEnteredValue);
    }
}

using System.Globalization;
using TMPro;
using UnityEngine;

public class SettingsInputFieldHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField input;
    [SerializeField] private UserSettings.SettingProperty property;

    private void Start()
    {
        ForceUpdate();
        input.onValueChanged.AddListener(HandleChange);
    }

    public void ForceUpdate()
    {
        input.text = Chart.settings.GetChartingSetting(property).ToString(CultureInfo.InvariantCulture);
    }

    private void HandleChange(string input)
    {
        // Converts to float b/c these input fields are just "numbers" and are casted to int when needed anyway. 
        // Remember to restrict to the correct type of input on the input fields.
        if (int.TryParse(input, out var result))
        {
            Chart.settings.SetChartingSetting(property, result);
        }
    }
}
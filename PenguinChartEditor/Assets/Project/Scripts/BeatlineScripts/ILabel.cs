using TMPro;
using UnityEngine;

public interface ILabel
{
    GameObject Label { get; set; }
    RectTransform LabelRectTransform { get; set; }
    TMP_InputField LabelEntryBox { get; set; }
    TextMeshProUGUI _labelText { get; set; }

    string LabelText
    {
        get
        {
            return _labelText.text;
        }
        set
        {
            _labelText.text = value;
        }
    }
}
using UnityEngine;
using TMPro;

public class BPMLabel : Event, ILabel
{
    [field: SerializeField] public GameObject Label { get; set; }
    [field: SerializeField] public RectTransform LabelRectTransform { get; set; }
    [field: SerializeField] public TMP_InputField LabelEntryBox { get; set; }
    [field: SerializeField] public TextMeshProUGUI _labelText { get; set; }

}
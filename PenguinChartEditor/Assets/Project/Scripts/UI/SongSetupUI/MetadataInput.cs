using System;
using TMPro;
using UnityEngine;

public class MetadataInput : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;
    [SerializeField] private Metadata.MetadataType targetMetadata;

    private void Awake()
    {
        if (Chart.Metadata.SongInfo.TryGetValue(targetMetadata, out var value))
        {
            inputField.text = value;
        }

        inputField.onValueChanged.AddListener(UpdateMetadata);
    }

    private void UpdateMetadata(string input)
    {
        Chart.Metadata.SongInfo[targetMetadata] = input;
    }
}

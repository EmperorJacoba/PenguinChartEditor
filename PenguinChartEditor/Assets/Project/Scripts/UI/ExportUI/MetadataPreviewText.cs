using TMPro;
using UnityEngine;

public class MetadataPreviewText : MonoBehaviour
{
    [SerializeField] private bool isSongLength = false;
    [SerializeField] private Metadata.MetadataType representedField;
    private TMP_Text textComponent;

    private void Start()
    {
        textComponent = GetComponent<TMP_Text>();
        if (isSongLength)
        {
            textComponent.text = SongTime.ConvertSecondsToTimestamp(SongTime.SongLength, false);
            return;
        }
        textComponent.text = $"{Chart.Metadata.SongInfo[representedField]}";
    }
}
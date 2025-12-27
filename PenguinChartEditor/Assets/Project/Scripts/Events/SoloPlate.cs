using TMPro;
using UnityEngine;

public class SoloPlate : MonoBehaviour
{
    [SerializeField] TMP_Text percentage;
    [SerializeField] TMP_Text counter;

    public void UpdatePositionAndText(float zPosition, int notesHit, int totalNotes)
    {
        transform.position = new(transform.position.x, transform.position.y, zPosition);
        percentage.text = $"{Mathf.Floor((notesHit / (float)totalNotes) * 100)}%";
        counter.text = $"{notesHit} / {totalNotes}";
    }
}

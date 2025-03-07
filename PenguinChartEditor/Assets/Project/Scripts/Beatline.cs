using TMPro;
using UnityEngine;

/// <summary>
/// The script attached to the beatline prefab. 
/// <para>The beatline prefab is a UI element with a line renderer with two points set to the width of the track.</para>
/// </summary>
public class Beatline : MonoBehaviour
{
    /// <summary>
    /// The BPM label text on the beatline's label.
    /// </summary>
    private TextMeshProUGUI bpmLabel;

    /// <summary>
    /// The container for the label object and the label text.
    /// </summary>
    private GameObject beatlineLabel;
    private RectTransform beatlineLabelRt;

    /// <summary>
    /// The line renderer attached to the beatline.
    /// </summary>
    private LineRenderer line;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        beatlineLabel = transform.GetChild(0).gameObject;
        beatlineLabelRt = beatlineLabel.GetComponent<RectTransform>();
        bpmLabel = beatlineLabel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        line = GetComponent<LineRenderer>();
    }

    public void UpdateBeatlinePosition(float newYPos) // change this to percentage later
    {
        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector2(line.GetPosition(0).x, newYPos);
        newPos[1] = new Vector2(line.GetPosition(1).x, newYPos);
        line.SetPositions(newPos);
        UpdateBeatlinePosition();
    }

    private void UpdateBeatlinePosition()
    {
        beatlineLabel.transform.localPosition = new Vector3(beatlineLabel.transform.localPosition.x, line.GetPosition(1).y - (beatlineLabelRt.rect.height / 2));
    }

    public void HideBeatlineLabel()
    {
        beatlineLabel.SetActive(false);
    }

    public void ShowBeatlineLabel()
    {
        beatlineLabel.SetActive(true);
        UpdateBeatlinePosition();
    }

    public void UpdateBPMLabelText(float newBPM)
    {
        bpmLabel.text = newBPM.ToString();
    }
    
}

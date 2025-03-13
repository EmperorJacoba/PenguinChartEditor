using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The script attached to the beatline prefab. 
/// <para>The beatline prefab is a UI element with a line renderer with two points set to the width of the track.</para>
/// <remarks>Beatline game object control should happen through this class.</remarks>
/// </summary>
public class Beatline : MonoBehaviour
{
    #region Components

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
    /// The line renderer attached to the beatline game object.
    /// </summary>
    private LineRenderer line;

    private RectTransform beatlineRt;

    #endregion

    public bool IsVisible
    {
        get 
        {
            return gameObject.activeInHierarchy;
        }
        set 
        {
            gameObject.SetActive(value);
        }
    }

    #region Functions 

    /// <summary>
    /// Update the position of the beatline to a specified proportion up the screen.
    /// </summary>
    /// <param name="percentOfScreen">The percent of the screen that should exist between the bottom and the beatline.</param>
    public void UpdateBeatlinePosition(float percentOfScreen) // change this to percentage later
    {
        // rect.height does not work here because the underlying rectangle of this game object has w*h of (0,0)
        // Size delta is the negative of the screen size because that's (0 - screen size)
        var newYPos = percentOfScreen * -beatlineRt.sizeDelta.y; 

        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector2(line.GetPosition(0).x, newYPos);
        newPos[1] = new Vector2(line.GetPosition(1).x, newYPos);
        line.SetPositions(newPos);
        UpdateLabel();
    }
    
    public void HideBeatlineLabel()
    {
        beatlineLabel.SetActive(false);
    }

    public void ShowBeatlineLabel()
    {
        beatlineLabel.SetActive(true);
        UpdateLabel();
    }

    public void UpdateBPMLabelText(float newBPM)
    {
        bpmLabel.text = newBPM.ToString();
    }

    #endregion
    
    #region Internal Functions
    private void UpdateLabel()
    {
        beatlineLabel.transform.localPosition = new Vector3(beatlineLabel.transform.localPosition.x, line.GetPosition(1).y - (beatlineLabelRt.rect.height / 2));
    }

    void Awake()
    {
        beatlineRt = gameObject.GetComponent<RectTransform>();
        beatlineLabel = transform.GetChild(0).gameObject;
        beatlineLabelRt = beatlineLabel.GetComponent<RectTransform>();
        bpmLabel = beatlineLabel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        line = gameObject.GetComponent<LineRenderer>();
    }

    #endregion
}

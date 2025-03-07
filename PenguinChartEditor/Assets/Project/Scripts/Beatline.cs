using TMPro;
using UnityEngine;

public class Beatline : MonoBehaviour
{
    /// <summary>
    /// The BPM label on the beatline's label.
    /// </summary>
    public TextMeshProUGUI BPMLabel {get; private set;}

    /// <summary>
    /// The container for the label object and the label text.
    /// </summary>
    public GameObject BeatlineLabel {get; private set;}

    /// <summary>
    /// The line renderer attached to the beatline.
    /// </summary>
    public LineRenderer Line {get; private set;}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BeatlineLabel = transform.GetChild(0).gameObject;
        BPMLabel = BeatlineLabel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        Line = GetComponent<LineRenderer>();
    }

    // BIG ISSUE:
    // How the hell can the width get right? ask GPT? ask internet? how to spawn with correct width?
    // AND update as window size changes??
}

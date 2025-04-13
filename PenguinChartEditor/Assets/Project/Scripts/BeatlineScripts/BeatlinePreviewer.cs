using UnityEngine;

public class BeatlinePreviewer : MonoBehaviour
{
    [SerializeField] Beatline beatline;
    void Start()
    {
        beatline.Type = Beatline.BeatlineType.none;
        beatline.BPMLabelVisible = false;
        beatline.TSLabelVisible = false;
    }

    void UpdatePreviewPosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        // get current cursor position
        // calculate the timestamp and following tick time
        // use current division to round that tick time to a current tick
        // place this beatline at that ticktime
    }
}

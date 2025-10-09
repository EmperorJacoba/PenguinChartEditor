using UnityEngine;

public class TrackBackground : MonoBehaviour
{
    [SerializeField] GameObject screenReference;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.localScale = screenReference.transform.
        transform.position = screenReference.transform.position;

        // I have no idea why but the waveform will not show up unless the plane moves back after initializing
    }

}

using UnityEngine;

public class TrackBackground : MonoBehaviour
{
    [SerializeField] GameObject screenReference;

    void Start()
    {
        transform.localScale = screenReference.transform.
        transform.position = screenReference.transform.position;
    }
}

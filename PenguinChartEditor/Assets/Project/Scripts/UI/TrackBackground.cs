using UnityEngine;

public class TrackBackground : MonoBehaviour
{
    [SerializeField] private GameObject screenReference;

    private void Start()
    {
        transform.localScale = screenReference.transform.
        transform.position = screenReference.transform.position;
    }
}

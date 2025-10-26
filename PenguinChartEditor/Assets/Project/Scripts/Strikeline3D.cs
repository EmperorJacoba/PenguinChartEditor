using UnityEngine;

public class Strikeline3D : MonoBehaviour, IStrikeline
{
    public static Strikeline3D instance;

    // highway.localScale = length of track
    [SerializeField] Transform highway;

    void Awake()
    {
        instance = this;
    }

    public float GetStrikelineProportion()
    {
        return transform.localPosition.z / highway.localScale.z;
    }
}
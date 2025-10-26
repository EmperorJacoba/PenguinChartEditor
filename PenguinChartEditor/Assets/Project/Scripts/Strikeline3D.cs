using UnityEngine;

public class Strikeline3D : MonoBehaviour, IStrikeline
{
    public static Strikeline3D instance;
    [SerializeField] Transform highway;

    void Awake()
    {
        instance = this;
    }

    public float GetStrikelineProportion()
    {

    }
}
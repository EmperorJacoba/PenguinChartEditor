using UnityEngine;

public class Strikeline3D : MonoBehaviour
{
    public GameInstrument parentGameInstrument;

    // In order for this to work properly, two conditions must be met.
    // 1. Instrument's Z position is set to 0.
    // 2. Highway's Z local Z position is set to 0.
    // This is to keep highwayLength (a property based on the Z scale of the highway)
    public static float GetStrikelineProportion()
    {
        return StrikelinePosition / Highway3D.highwayLength;
    }

    public delegate void StrikelinePositionDelegate();
    public static event StrikelinePositionDelegate StrikelinePositionUpdated;
    public static float StrikelinePosition
    {
        get
        {
            return _sp;
        }
        set
        {
            if (_sp == value) return;

            _sp = value;
            StrikelinePositionUpdated?.Invoke();
        }
    }
    private static float _sp = 7.5f;

    void Awake()
    {
        UpdateStrikelinePosition();
        StrikelinePositionUpdated += UpdateStrikelinePosition;
    }

    void UpdateStrikelinePosition()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, StrikelinePosition);
    }
}
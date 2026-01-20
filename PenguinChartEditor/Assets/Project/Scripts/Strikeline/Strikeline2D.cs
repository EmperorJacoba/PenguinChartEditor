using UnityEngine;

public interface IStrikeline
{
    float GetStrikelineProportion();
}

/// <summary>
/// Class attached to the Strikeline container game object. 
/// </summary>
public class Strikeline2D : MonoBehaviour, IStrikeline
{
    public static Strikeline2D instance;
    [SerializeField] private GameInstrument parentGameInstrument;
    [SerializeField] private RectTransform strikelineRt;
    [SerializeField] private RectTransform boundaryReference;

    private void Awake()
    {
        instance = this;
        parentGameInstrument.strikeline = this;
    }
    
    /// <summary>
    /// Calculate the proportion of the screen that the strikline is up by
    /// <para>Example: 0.1 => the strikeline is 10% up from the bottom of the screen relative to the screen</para>
    /// </summary>
    /// <returns>Proportion as decimal</returns>
    public float GetStrikelineProportion()
    {
        // constraints:
        // 0 is at bottom of screen/track, which is where anchor point/pivot MUST be
        // strikeline must be a child of the screen/track reference in order to get proper calculations (localPosition)
        var screenHeight = boundaryReference.rect.height;
        var strikelinePositionY = strikelineRt.localPosition.y; // this is the "Pos Y" in the editor, relative to screenHeight
        return strikelinePositionY / screenHeight; // this is the proportion of the screen that the strikeline is up by
    }
}

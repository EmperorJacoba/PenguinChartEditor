using UnityEngine;

/// <summary>
/// The script attached to the beatline prefab. 
/// <para>The beatline prefab is a UI element with a line renderer with two points set to the width of the track, and has malleable BPM and TS labels.</para>
/// <remarks>Beatline game object control should happen through this class.</remarks>
/// </summary>
public class Beatline : BaseBeatline
{
    /// <summary>
    /// Line renderer thicknesses corresponding to each beatline type in the BeatlineType enum. 
    /// </summary>
    protected override float[] thicknesses => _thicknesses;

    float[] _thicknesses = { 0, 0.05f, 0.02f, 0.005f };

    #region Properties

    private float yScreenProportion = 0;

    #endregion

    #region Functions

    /// <summary>
    /// Update the position of the beatline to a specified proportion up the screen.
    /// </summary>
    /// <param name="percentOfScreen">The percent of the screen that should exist between the bottom and the beatline.</param>
    public override void UpdateBeatlinePosition(double percentOfScreen, float screenHeight, GameInstrument parentGameInstrument)
    {
        // use screen ref to calculate percent of screen -> scale is 1:1 in the line renderer (scale must be 1, 1, 1)
        yScreenProportion = (float)(percentOfScreen * screenHeight);

        Vector3[] newPos = new Vector3[2];
        newPos[0] = new Vector2(line.GetPosition(0).x, (float)yScreenProportion);
        newPos[1] = new Vector2(line.GetPosition(1).x, (float)yScreenProportion);
        line.SetPositions(newPos);
    }

    #endregion
}
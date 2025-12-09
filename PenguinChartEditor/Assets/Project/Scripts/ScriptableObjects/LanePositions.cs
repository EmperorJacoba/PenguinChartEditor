using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu]
public class LanePositions : ScriptableObject
{
    public List<float> lanePositions;
    public bool openAsNormalLane;

    public float GetLaneWorldSpaceXCoordinate(int lanePosition)
    {
        if (lanePositions.Count > lanePosition)
        {
            return lanePositions[lanePosition];
        }
        else
        {
            Debug.LogWarning(
                $@"Lane positioning is not set up correctly for this charting mode. 
                Please update this lane positioning script to have the correct number of lanes for this mode. 
                Lane requested: {lanePosition}. Lane positions in {name} = {lanePositions.Count}.
                ");
            return 0;
        }
    }
}

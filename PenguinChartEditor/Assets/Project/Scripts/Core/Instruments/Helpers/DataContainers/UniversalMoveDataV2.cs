using System;
using System.Collections.Generic;
using UnityEngine;

public class UniversalMoveDataV2
{
    public bool inProgress;
    
    private int firstMouseTick;
    public int lastMouseTick;

    private int firstMouseLane;
    public int lastMouseLane;

    private int firstSelectionTick;
    
    private IMultiLaneController laneController;

    private ISelectionSnapshot originalMoveDataNormalized;

    public UniversalMoveDataV2(int firstMouseTick, int firstMouseLane, IMultiLaneController laneController)
    {
        this.firstMouseTick = lastMouseTick = firstMouseTick;
        this.firstMouseLane = lastMouseLane = firstMouseLane;
        
        this.laneController = laneController;
        
        if (laneController.IsSelectionEmpty()) return;
        firstSelectionTick = laneController.GetFirstSelectionTick();

        originalMoveDataNormalized = laneController.TakeNormalizedSelectionSnapshot();

        inProgress = true;
    }

    public UniversalMoveDataV2()
    {
        inProgress = false;
        lastMouseTick = -1;
        lastMouseLane = int.MinValue;
    }

    /// <remarks>Pass in null for laneProgression if there is no cross-lane movement.</remarks>
    public ISelectionSnapshot GetNewMoveDataLocation(LinkedList<int> laneProgression)
    {
        var tickDelta = lastMouseTick - firstMouseTick;
        var scaledSnapshot = originalMoveDataNormalized.ScaleSelectionSnapshot(firstSelectionTick + tickDelta);
        
        if (laneProgression is null) return scaledSnapshot;
        
        var laneDelta = lastMouseLane - firstMouseLane;
        var shiftedSnapshot = scaledSnapshot.ShiftSnapshotLanes(laneDelta, laneProgression);

        return shiftedSnapshot;
    }
}
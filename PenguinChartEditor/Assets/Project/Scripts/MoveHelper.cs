using System.Collections.Generic;
using UnityEngine;

public class MoveHelper<T> where T : IEventData
{
    private UniversalMoveData<T> moveData = new();

    public bool MoveInProgress => moveData.inProgress;
    public MinMaxTicks GetFinalValidationRange(LinkedList<int> laneProgression) => moveData.GetChangedDataRange(laneProgression);
    public MinMaxTicks GetChangingValidationRange() => new(moveData.lastGhostStartTick, moveData.lastGhostEndTick);


    public void Reset()
    {
        moveData = new();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instrument"></param>
    /// <param name="laneData"></param>
    /// <param name="laneProgression"></param>
    /// <returns>Were there any meaningful changes to the Lanes dataset?</returns>
    public bool Move2DSelection(IInstrument instrument, Lanes<T> laneData, LinkedList<int> laneProgression)
    {
        if (instrument != Chart.LoadedInstrument || !Chart.IsModificationAllowed()) return false;

        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() && !moveData.inProgress) return false;

        if (laneData.IsSelectionEmpty()) return false;

        bool tickMovement = false;
        bool laneMovement = false;

        var currentMouseTick = SongTime.CalculateGridSnappedTick(Chart.instance.SceneDetails.GetCursorHighwayProportion());
        var currentMouseLane = Chart.instance.SceneDetails.MatchXCoordinateToLane(Chart.instance.SceneDetails.GetCursorHighwayPosition().x);

        if (currentMouseTick != moveData.lastMouseTick)
        {
            moveData.lastMouseTick = currentMouseTick;
            tickMovement = true;
        }
        if (currentMouseLane != moveData.lastLane)
        {
            moveData.lastLane = currentMouseLane;
            laneMovement = true;
        }

        if (!moveData.inProgress && (tickMovement || laneMovement))
        {
            // optimize call
            moveData = new(
                currentMouseTick,
                currentLane: currentMouseLane,
                laneData
                );
            Chart.showPreviewers = false;
            return false;
        }

        if (!(tickMovement || laneMovement)) return false;

        laneData.OverwriteLaneData(moveData.preMoveData);

        var cursorMoveDifference = currentMouseTick - moveData.firstMouseTick;
        var pasteDestination = moveData.firstSelectionTick + cursorMoveDifference;
        moveData.lastGhostStartTick = pasteDestination;

        var movingDataSet = moveData.GetMoveData(currentMouseLane - moveData.firstLane, laneProgression);

        laneData.OverwriteLaneDataWithOffset(movingDataSet, pasteDestination);

        laneData.ApplyScaledSelection(movingDataSet, moveData.lastGhostStartTick);
        return true;
    }
}
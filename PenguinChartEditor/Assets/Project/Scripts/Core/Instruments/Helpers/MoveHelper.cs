using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveHelper<T> where T : IEventData
{
    private UniversalMoveData<T> moveData = new();

    public bool MoveInProgress => moveData.inProgress;

    public MinMaxTicks GetFinalValidationRange()
    {
        var selectionSnap = lastAddedData as SelectionSnapshot<T>;

        MinMaxTracker tracker = new MinMaxTracker(selectionSnap.savedSelectionData.Count);
        foreach (var lane in selectionSnap.savedSelectionData.Where(lane => lane.Value.Count != 0))
        {
            var min = lane.Value.Keys.Min();
            tracker.AddTickMinMax(min, lane.Value.Keys.Max());
        }

        return tracker.GetAbsoluteMinMax();
    }
    
    public MinMaxTicks GetChangingValidationRange() => new(moveData.lastGhostStartTick, moveData.lastGhostEndTick);

    private readonly IInstrument parentInstrument;
    
    public MoveSelectionSnapshot Reset()
    {
        openUndoAction.CloseAction(
            new AddDataInRangeSnapshot(parentInstrument,
                new AddDataInRangeDataPackage(
                    overwrittenData: lastOverwrittenData, 
                    incomingData: lastAddedData
                    )
                )
            );
        
        Chart.showPreviewers = true;
        moveData = new UniversalMoveData<T>();
        return openUndoAction;
    }

    public MoveHelper(IInstrument parentInstrument)
    {
        this.parentInstrument = parentInstrument;
    }

    private MoveSelectionSnapshot openUndoAction;

    private ISelectionSnapshot lastOverwrittenData;
    private ISelectionSnapshot lastAddedData;
    
    // 2D in this context means [lane X data] dataset (multiple lanes) - e.g. any traditional instrument (guitar)
    // 1D in this context means just one lane, no cross-LaneSet<> movement needed - e.g TempoEvents, Sections, etc.
    
    /// <returns>Were there any meaningful changes to the Lanes dataset?</returns>
    public bool Move2DSelection(Lanes<T> laneData, LinkedList<int> laneProgression, out bool actionStarted)
    {
        actionStarted = false;
        if (parentInstrument != Chart.LoadedInstrument || !Chart.IsModificationAllowed()) return false;

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
            openUndoAction = new MoveSelectionSnapshot(parentInstrument,
                new DeleteSelectionSnapshot(parentInstrument, laneData.TakeSelectionSnapshot()));
            
            moveData = new UniversalMoveData<T>(
                currentMouseTick,
                currentLane: currentMouseLane,
                laneData
                );
            
            Chart.showPreviewers = false;
            actionStarted = true;
            return false;
        }

        if (!(tickMovement || laneMovement)) return false;
        
        laneData.OverwriteAllLaneData(moveData.preMoveData);

        var cursorMoveDifference = currentMouseTick - moveData.firstMouseTick;
        var pasteDestination = moveData.firstSelectionTick + cursorMoveDifference;
        moveData.lastGhostStartTick = pasteDestination;

        var movingDataSet = moveData.GetMoveData(currentMouseLane - moveData.firstLane, laneProgression);
        lastAddedData = new SelectionSnapshot<T>(movingDataSet);
        
        laneData.AddTicksFromSet(movingDataSet, out lastOverwrittenData);

        laneData.ApplyScaledSelection(movingDataSet, moveData.lastGhostStartTick);
        return true;
    }

    public bool Move1DSelection(LaneSet<T> lane, SelectionSet<T> selection, out bool actionStarted)
    {
        actionStarted = false;
        if (parentInstrument != Chart.LoadedInstrument || !Chart.IsModificationAllowed()) return false;
        
        var currentMouseTick = SongTime.CalculateGridSnappedTick(Input.mousePosition.y / Screen.height);
        
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() && !moveData.inProgress)
        {
            return false;
        }

        if (currentMouseTick == moveData.lastMouseTick) 
        {
            return false;
        }

        if (!moveData.inProgress)
        {
            moveData = new UniversalMoveData<T>(
                currentMouseTick,
                lane,
                selection
            );
            Chart.showPreviewers = false;
            actionStarted = true;
            return false;
        }

        lane.OverwriteAllLaneDataWith(moveData.preMoveData[0]);

        var cursorMoveDifference = currentMouseTick - moveData.firstMouseTick;

        var pasteDestination = moveData.firstSelectionTick + cursorMoveDifference;
        moveData.lastGhostStartTick = pasteDestination;

        var movingDataSet = moveData.OneDGetMoveData()[0];

        lane.OverwriteDataWithOffset(movingDataSet, pasteDestination);

        selection.ApplyScaledSelection(movingDataSet, moveData.lastGhostStartTick);

        moveData.lastMouseTick = currentMouseTick;

        return true;
    }

    public void ForceMoveStart()
    {
        moveData.inProgress = true;
    }
}
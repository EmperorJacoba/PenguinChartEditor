using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveHelper<T> where T : IEventData
{
    private UniversalMoveData<T> moveData = new();
    private UniversalMoveDataV2 moveDataV2 = new();

    public bool MoveInProgress => moveDataV2.inProgress;

    public MinMaxTicks GetFinalValidationRange()
    {
        var selectionSnap = lastMoveAction.actionInfo.incomingData as SelectionSnapshot<T>;

        MinMaxTracker tracker = new MinMaxTracker(selectionSnap.savedSelectionData.Count);
        foreach (var lane in selectionSnap.savedSelectionData.Where(lane => lane.Value.Count != 0))
        {
            var min = lane.Value.Keys.Min();
            tracker.AddTickMinMax(min, lane.Value.Keys.Max());
        }

        return tracker.GetAbsoluteMinMax();
    }

    // FIXME: Change back to the start and end points of the moving data set.
    public MinMaxTicks GetChangingValidationRange() => new(0, SongTime.SongLengthTicks);

    private readonly IInstrument parentInstrument;
    
    public MoveSelectionSnapshot Reset()
    {
        openUndoAction.CloseAction(
            lastMoveAction
            );
        
        Chart.showPreviewers = true;
        moveDataV2 = new UniversalMoveDataV2();
        
        return openUndoAction;
    }

    public MoveHelper(IInstrument parentInstrument)
    {
        this.parentInstrument = parentInstrument;
    }

    private MoveSelectionSnapshot openUndoAction;
    
    private AddDataInRangeSnapshot lastMoveAction;

    /// Pass in laneProgression as null to single a 1-dimensional movement.
    public bool MoveSelection(IMultiLaneController laneController, LinkedList<int> laneProgression)
    {
        if (
            parentInstrument != Chart.LoadedInstrument || 
            !Chart.IsModificationAllowed() || 
            laneController.IsSelectionEmpty()
            ) 
            return false;
        
        if (
            Chart.instance.SceneDetails.IsSceneOverlayUIHit() && 
            !moveDataV2.inProgress
            ) 
            return false;

        var tickChange = IsMoveTickChange(out moveDataV2.lastMouseTick);
        var laneChange = IsMoveLaneChange(out moveDataV2.lastMouseLane);

        if (!tickChange && !laneChange) return false;

        if (!moveDataV2.inProgress)
        {
            OpenUndoAction(laneController);

            moveDataV2 = new UniversalMoveDataV2(
                moveDataV2.lastMouseTick,
                moveDataV2.lastMouseLane,
                laneController
            );

            lastMoveAction = new AddDataInRangeSnapshot(
                parentInstrument, 
                new AddDataInRangeDataPackage(
                    // There is no data to overwrite. This is the initial action that effectively "pops"
                    // the move data out of the lane controller. It has to reinstate nothing.
                    new SelectionSnapshot<T>(new Dictionary<int, SortedDictionary<int, T>>()), 
                    moveDataV2.GetNewMoveDataLocation(null)
                    )
                );

            Chart.showPreviewers = false;
            return false;
        }
        
        lastMoveAction.Undo();

        var newMoveData = moveDataV2.GetNewMoveDataLocation(laneProgression);
        
        laneController.AddTicksFromSet(newMoveData, out var overwrittenData);
        laneController.SelectTicksFromSnapshot(newMoveData);

        lastMoveAction = new AddDataInRangeSnapshot(parentInstrument,
            new AddDataInRangeDataPackage(overwrittenData, newMoveData));
        
        return true;
    }

    private bool IsMoveTickChange(out int currentMouseTick)
    {
        currentMouseTick = SongTime.CalculateCurrentMouseTick();

        return currentMouseTick != moveData.lastMouseTick;
    }

    private bool IsMoveLaneChange(out int currentLane)
    {
        currentLane = Chart.instance.SceneDetails.MatchXCoordinateToLane(
            Chart.instance.SceneDetails.GetCursorHighwayPosition().x
            );

        return currentLane != moveData.lastLane;
    }

    private void OpenUndoAction(IMultiLaneController laneController)
    {
        openUndoAction = new MoveSelectionSnapshot(parentInstrument,
            new DeleteSelectionSnapshot(parentInstrument, laneController.TakeSelectionSnapshot()));
    }

    #region Deprecated
    
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

        var tickDelta = currentMouseTick - moveData.firstMouseTick;
        moveData.lastGhostStartTick = moveData.firstSelectionTick + tickDelta;

        var movingDataSet = moveData.GetMoveData(currentMouseLane - moveData.firstLane, laneProgression);
        
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
    
    #endregion

    public void ForceMoveStart()
    {
        moveData.inProgress = true;
    }
}
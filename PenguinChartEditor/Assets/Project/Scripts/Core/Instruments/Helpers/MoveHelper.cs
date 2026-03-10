using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveHelper<T> where T : IEventData
{
    private UniversalMoveDataV2 moveData = new();
    private readonly IInstrument parentInstrument;
    public bool MoveInProgress => moveData.inProgress;
    
    private MoveSelectionSnapshot openUndoAction;
    private AddDataInRangeSnapshot lastMoveAction;
    
    private void OpenUndoAction(IMultiLaneController laneController)
    {
        openUndoAction = new MoveSelectionSnapshot(parentInstrument,
            new DeleteSelectionSnapshot(parentInstrument, laneController.TakeSelectionSnapshot()));
    }

    public MinMaxTicks GetFinalValidationRange()
    {
        var selectionSnap = lastMoveAction.actionInfo.incomingData as SelectionSnapshot<T>;
        Debug.Assert(selectionSnap is not null);

        var tracker = new MinMaxTracker(selectionSnap.savedSelectionData.Count);
        foreach (var lane in selectionSnap.savedSelectionData.Where(lane => lane.Value.Count != 0))
        {
            var min = lane.Value.Keys.Min();
            tracker.AddTickMinMax(min, lane.Value.Keys.Max());
        }

        return tracker.GetAbsoluteMinMax();
    }

    // FIXME: Change back to the start and end points of the moving data set. 
    public MinMaxTicks GetChangingValidationRange() => new(0, SongTime.SongLengthTicks);
    
    public MoveSelectionSnapshot Reset()
    {
        openUndoAction.CloseAction(lastMoveAction);
        
        Chart.showPreviewers = true;
        moveData = new UniversalMoveDataV2();
        
        return openUndoAction;
    }

    public void SaveCutoffSustainData(IMultiLaneController laneController)
    {
        var selectionSnap = lastMoveAction.actionInfo.incomingData as SelectionSnapshot<T>;
        Debug.Assert(selectionSnap is not null);

        var sustainableInstrument = parentInstrument as ISustainableInstrument;
        Debug.Assert(sustainableInstrument is not null);

        Dictionary<int, SortedDictionary<int, T>> savedSustainData = new();
        Dictionary<int, SortedDictionary<int, T>> postMoveSustainData = new();

        foreach (var lane in selectionSnap.savedSelectionData)
        {
            if (lane.Value.Count == 0) continue;
            
            var checkTick = laneController.GetLane(lane.Key).GetPreviousTickEventInLane(lane.Value.Keys.First());
            if (!laneController.TryGetTick(lane.Key, checkTick, out var data)) return;

            var sustainData = data as ISustainable;
            Debug.Assert(sustainData is not null);

            var clampedSustainValue =
                sustainableInstrument.CalculateSustainClamp(sustainData.Sustain, checkTick, lane.Key);
            
            if (clampedSustainValue != sustainData.Sustain)
            {
                savedSustainData[lane.Key] = new SortedDictionary<int, T>();
                postMoveSustainData[lane.Key] = new SortedDictionary<int, T>();
                
                savedSustainData[lane.Key].Add(checkTick, (T)data);

                sustainData.Sustain = clampedSustainValue;
                
                postMoveSustainData[lane.Key].Add(checkTick, (T)sustainData);
            }
        }

        openUndoAction.sustainAction = 
            new AddDataInRangeSnapshot(
                parentInstrument,
                new AddDataInRangeDataPackage(
                    new SelectionSnapshot<T>(savedSustainData), 
                    new SelectionSnapshot<T>(postMoveSustainData)
                    )
                );
    }
    
    public MoveHelper(IInstrument parentInstrument)
    {
        this.parentInstrument = parentInstrument;
    }

    /// <remarks>Pass in laneProgression as null to signal a 1-dimensional (no cross-lane) movement.</remarks>
    /// <returns>Were there any meaningful changes to the Lanes dataset?</returns>
    public bool MoveSelection(IMultiLaneController laneController, LinkedList<int> laneProgression)
    {
        // Basic discriminates when moving does not apply
        if (
            parentInstrument != Chart.LoadedInstrument || 
            !Chart.IsModificationAllowed() || 
            laneController.IsSelectionEmpty()
            ) 
            return false;
        
        // Don't start a move if over UI elements. But OK if a move is already happening. Don't stop in the middle.
        if (
            Chart.instance.SceneDetails.IsSceneOverlayUIHit() && 
            !moveData.inProgress
            ) 
            return false;

        // Please do not sneak the moveData variable assignments directly into the function calls. It assigns the 
        // field of moveData before the comparison and it will always yield false as a result.
        // This is a stupid "feature," John Michaelsoft!! Why can't it just be assigned after the function terminates?
        var tickChange = IsMoveTickChange(out var currentTick);
        var laneChange = IsMoveLaneChange(out var currentLane);

        moveData.lastMouseTick = currentTick;
        moveData.lastMouseLane = currentLane;
        
        if (!tickChange && !laneChange) return false;

        if (!moveData.inProgress)
        {
            OpenUndoAction(laneController);

            moveData = new UniversalMoveDataV2(
                moveData.lastMouseTick,
                moveData.lastMouseLane,
                laneController
            );

            lastMoveAction = new AddDataInRangeSnapshot(
                parentInstrument, 
                new AddDataInRangeDataPackage(
                    // There is no data to overwrite. This is the initial action that effectively "pops"
                    // the move data out of the lane controller. It has to reinstate nothing.
                    new SelectionSnapshot<T>(new Dictionary<int, SortedDictionary<int, T>>()), 
                    moveData.GetNewMoveDataLocation(null)
                    )
                );

            Chart.showPreviewers = false;
            return false;
        }
        
        lastMoveAction.Undo();

        var newMoveData = moveData.GetNewMoveDataLocation(laneProgression);
        
        laneController.AddTicksFromSet(newMoveData, out var overwrittenData);
        
        // moving data remains selected through and through! such a hearty opponent for the deselectorifier. 
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

        return currentLane != moveData.lastMouseLane;
    }
}
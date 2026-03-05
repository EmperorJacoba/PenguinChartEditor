using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SustainData<T> where T : IEventData
{
    public readonly bool sustainInProgress;
    public int lastMouseTick;
    public readonly Dictionary<int, HashSet<int>> sustainingTicks;

    private readonly SustainSelectionSnapshot undoAction;
    private Lanes<T> laneController;

    public SustainData(IInstrument parentInstrument, Lanes<T> laneController, int mouseTick)
    {
        lastMouseTick = mouseTick;
        sustainingTicks = laneController.GetTotalSelectionByLane();
        this.laneController = laneController;
        
        undoAction = new SustainSelectionSnapshot(parentInstrument, laneController.TakeSelectionSnapshot());
        
        sustainInProgress = true;
    }

    public void CompleteAction()
    {
        undoAction.CloseAction(laneController.TakeSelectionSnapshot());
        UndoStack.instance.PushAction(undoAction);
    }

    // use only in Lane<T> class/end of user sustain --
    // this is supposed to signal for the sustain function
    // that this object must be properly initialized with the new loop's variables
    public SustainData()
    {
        sustainInProgress = false;
    }
}
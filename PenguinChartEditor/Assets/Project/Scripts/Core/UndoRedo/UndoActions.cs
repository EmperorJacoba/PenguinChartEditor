using System;
using System.Collections.Generic;

#region AddSingle

// Corresponds to CreateEvent()
public class AddSingleUndoSnapshot : IUndoSnapshot
{
    // Save overwritten data, if it exists and varies. Delete tick in lane and reinstate old note for undo, vice versa for redo 
    
    public IInstrument parentInstrument { get; }
    private AddSingleDataPackage actionInfo;
    
    public void Undo()
    {
        parentInstrument.UndoAdd(actionInfo);
    }

    public void Redo()
    {
        parentInstrument.RedoAdd(actionInfo);
    }

    public AddSingleUndoSnapshot(IInstrument parentInstrument, AddSingleDataPackage actionInfo)
    {
        this.parentInstrument = parentInstrument;
        this.actionInfo = actionInfo;
    }
}

public struct AddSingleDataPackage
{
    public readonly IEventData addedData;
    public readonly IEventData removedData;
    public readonly bool removedDataExists;
    public readonly int tick;
    public readonly int lane;
    
    public AddSingleDataPackage(int tick, int lane, IEventData addedData, IEventData removedData)
    {
        this.tick = tick;
        this.lane = lane;
        this.addedData = addedData;
        this.removedData = removedData;
        removedDataExists = true;
    }

    public AddSingleDataPackage(int tick, int lane, IEventData addedData)
    {
        this.tick = tick;
        this.lane = lane;
        this.addedData = addedData;
        removedData = default; // Use default! IEventData is a struct! Not nullable!!
        removedDataExists = false;
    }
}

#endregion

#region DeleteSingle

// Corresponds to DeleteTickInLane()
public class DeleteSingleUndoSnapshot : IUndoSnapshot
{
    // Save deleted note. Reinstate deleted note for undo, delete tick in lane for redo.
    public IInstrument parentInstrument { get; }
    private readonly DeleteSingleDataPackage actionInfo;
    
    public void Undo()
    {
        parentInstrument.UndoDeleteSingle(actionInfo);
    }

    public void Redo()
    {
        parentInstrument.RedoDeleteSingle(actionInfo);
    }

    public DeleteSingleUndoSnapshot(IInstrument parentInstrument, DeleteSingleDataPackage actionInfo)
    {
        this.parentInstrument = parentInstrument;
        this.actionInfo = actionInfo;
    }
}

public struct DeleteSingleDataPackage
{
    public readonly int tick;
    public readonly int lane;
    public readonly IEventData deletedData;

    public DeleteSingleDataPackage(int tick, int lane, IEventData deletedData)
    {
        this.tick = tick;
        this.lane = lane;
        this.deletedData = deletedData;
    }
}

#endregion

#region SelectionChange

// Corresponds to any in-place selection change. Examples: Setting a selection to all taps/hopos
public class SelectionChangeSnapshot : IUndoSnapshot
{
    // Save selection with changes (for redo), save without changes (for undo), apply as needed
    public IInstrument parentInstrument { get; }
    private IMultiLaneController laneController;

    private ISelectionSnapshot oldData;
    private ISelectionSnapshot addedData;
    
    public void Undo()
    {
        parentInstrument.ReinstateSelectionChange(oldData, addedData);
    }

    public void Redo()
    {
        parentInstrument.ReinstateSelectionChange(addedData, oldData);
    }

    public SelectionChangeSnapshot(IInstrument parentInstrument, IMultiLaneController laneController)
    {
        this.parentInstrument = parentInstrument;
        this.laneController = laneController;

        oldData = laneController.TakeSelectionSnapshot();
    }

    public void CloseAction() => addedData = laneController.TakeSelectionSnapshot();
}

#endregion

#region DeleteSelection

// Corresponds to DeleteSelection()
public class DeleteSelectionSnapshot : IUndoSnapshot
{
    // Save selection. Reinstate selection for undo, delete ticks in selection for redo
    public IInstrument parentInstrument { get; }
    private readonly ISelectionSnapshot actionInfo;
    
    public void Undo()
    {
        parentInstrument.UndoDeleteSelection(actionInfo);
    }

    public void Redo()
    {
        parentInstrument.RedoDeleteSelection(actionInfo);
    }

    public DeleteSelectionSnapshot(IInstrument parentInstrument, ISelectionSnapshot actionInfo)
    {
        this.parentInstrument = parentInstrument;
        this.actionInfo = actionInfo;
    }
}

#endregion

public class PasteSnapshot : IUndoSnapshot
{
    // Save notes falling in paste range. Delete notes in paste range and reinstate old notes for undo, vice versa for redo
    public IInstrument parentInstrument { get; }
    private PasteDataPackage actionInfo;
    public void Undo()
    {
        parentInstrument.ReinstateSelectionChange(actionInfo.prePasteData, actionInfo.pasteData);
    }

    public void Redo()
    {
        parentInstrument.ReinstateSelectionChange(actionInfo.pasteData, actionInfo.prePasteData);
    }

    public PasteSnapshot(IInstrument parentInstrument, PasteDataPackage actionInfo)
    {
        this.parentInstrument = parentInstrument;
        this.actionInfo = actionInfo;
    }
}

public struct PasteDataPackage
{
    public readonly ISelectionSnapshot prePasteData;
    public readonly ISelectionSnapshot pasteData;

    public PasteDataPackage(ISelectionSnapshot prePasteData, ISelectionSnapshot pasteData)
    {
        this.prePasteData = prePasteData;
        this.pasteData = pasteData;
    }
}

#region SingleSustain (sustain from sustain trail)

// Corresponds to changing the sustain from a tail.
public class SingleSustainSnapshot : IUndoSnapshot
{
    // Save original sustain. Reinstate original sustain for undo, reinstate new sustain for redo.
    public IInstrument parentInstrument { get; }
    private ISustainableInstrument parentSustainableInstrument => parentInstrument as ISustainableInstrument;
    
    private readonly SingleSustainDataPackage actionInfo;
    
    public void Undo()
    {
        parentSustainableInstrument.UndoSingleSustain(actionInfo);
    }

    public void Redo()
    {
        parentSustainableInstrument.RedoSingleSustain(actionInfo);
    }

    public SingleSustainSnapshot(IInstrument parentInstrument, SingleSustainDataPackage actionInfo)
    {
        this.parentInstrument = parentInstrument;
        this.actionInfo = actionInfo;
    }

    public void CloseAction() => actionInfo.SaveAddedSustainData();
}

public class SingleSustainDataPackage
{
    public readonly int tick;
    public readonly Dictionary<int, IEventData> oldData;
    public Dictionary<int, IEventData> addedData;

    private readonly IMultiLaneController parentLaneController;

    public SingleSustainDataPackage(int tick, IMultiLaneController laneController)
    {
        this.tick = tick;
        parentLaneController = laneController;
        oldData = laneController.GetAllTickDataAtTick(tick);
    }

    public void SaveAddedSustainData()
    {
        addedData = parentLaneController.GetAllTickDataAtTick(tick);
    }
}

#endregion

// Corresponds to SustainSelection()
public class SustainSelectionSnapshot : IUndoSnapshot
{
    // Save selection's original data. Reinstate old data for undo, reinstate new data for redo.
    public IInstrument parentInstrument { get; }
    public void Undo()
    {
        throw new System.NotImplementedException();
    }

    public void Redo()
    {
        throw new System.NotImplementedException();
    }
}

public class MoveSelectionSnapshot : IUndoSnapshot
{
    // Save maximum range of data (between paste location and original location of notes). Swap ranges out for undo/redo.
    public IInstrument parentInstrument { get; }
    public void Undo()
    {
        throw new System.NotImplementedException();
    }

    public void Redo()
    {
        throw new System.NotImplementedException();
    }
}

public class BPMDragChangeSnapshot : IUndoSnapshot
{
    // Save BPM event pre-drag. Undo restores old data, redo restores dragged data.
    public IInstrument parentInstrument { get; }
    public void Undo()
    {
        throw new System.NotImplementedException();
    }

    public void Redo()
    {
        throw new System.NotImplementedException();
    }
}

public class InputFieldEditSnapshot : IUndoSnapshot
{
    public IInstrument parentInstrument { get; }
    public void Undo()
    {
        throw new System.NotImplementedException();
    }

    public void Redo()
    {
        throw new System.NotImplementedException();
    }
}
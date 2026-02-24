// Corresponds to CreateEvent()
public class SingleAddSnapshot : IUndoSnapshot
{
    // Save overwritten data, if it exists and varies. Delete tick in lane and reinstate old note for undo, vice versa for redo 
    public IInstrument parentInstrument { get; }
    private AddDataPackage actionInfo;
    
    public void Undo()
    {
        parentInstrument.UndoAdd(actionInfo);
    }

    public void Redo()
    {
        parentInstrument.RedoAdd(actionInfo);
    }

    public SingleAddSnapshot(IInstrument parentInstrument, AddDataPackage actionInfo)
    {
        this.parentInstrument = parentInstrument;
        this.actionInfo = actionInfo;
    }
}

public struct AddDataPackage
{
    public IEventData addedData;
    public IEventData removedData;
    public bool removedDataExists;
    public int tick;
    public int lane;
    
    public AddDataPackage(int tick, int lane, IEventData addedData, IEventData removedData)
    {
        this.tick = tick;
        this.lane = lane;
        this.addedData = addedData;
        this.removedData = removedData;
        removedDataExists = true;
    }

    public AddDataPackage(int tick, int lane, IEventData addedData)
    {
        this.tick = tick;
        this.lane = lane;
        this.addedData = addedData;
        removedData = default;
        removedDataExists = false;
    }
}

// Corresponds to DeleteTickInLane()
public class SingleDeleteSnapshot : IUndoSnapshot
{
    // Save deleted note. Reinstate deleted note for undo, delete tick in lane for redo.
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

// Corresponds to any by-and-large selection change. Examples: Setting a selection to all taps/hopos, applying equal spacing
public class SelectionChangeSnapshot : IUndoSnapshot
{
    // Save selection with changes (for redo), save without changes (for undo), apply as needed
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

// Corresponds to DeleteSelection()
public class DeleteSelectionSnapshot : IUndoSnapshot
{
    // Save selection. Reinstate selection for undo, delete ticks in selection for redo
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

public class PasteSnapshot : IUndoSnapshot
{
    // Save notes falling in paste range. Delete notes in paste range and reinstate old notes for undo, vice versa for redo
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

// Corresponds to changing the sustain from a tail.
public class SingleSustainSnapshot : IUndoSnapshot
{
    // Save original sustain. Reinstate original sustain for undo, reinstate new sustain for redo.
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
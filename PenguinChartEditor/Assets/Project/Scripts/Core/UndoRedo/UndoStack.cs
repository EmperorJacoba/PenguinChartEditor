using System;
using System.Collections.Generic;
using UnityEngine;

public class UndoStack : MonoBehaviour
{
    private FiniteStack<IUndoSnapshot> undoStack;
    private FiniteStack<IUndoSnapshot> redoStack;

    public static UndoStack instance;

    private InputMap inputMap;
    private void Awake()
    {
        instance = this;
        instance.undoStack = new FiniteStack<IUndoSnapshot>(UserSettings.MaximumSavedUndoActions);
        instance.redoStack = new FiniteStack<IUndoSnapshot>(UserSettings.MaximumSavedUndoActions);

        inputMap = new InputMap();
        inputMap.Enable();
        inputMap.ExternalCharting.Undo.performed += _ => Undo();
        inputMap.ExternalCharting.Redo.performed += _ => Redo();
    }

    public void PushAction(IUndoSnapshot undoSnapshot)
    {
        undoStack.Push(undoSnapshot);
        redoStack.Clear();
    }

    private void Undo()
    {
        if (undoStack.Count == 0) return;
        
        var undoAction = undoStack.Pop();
        undoAction.Undo();
        redoStack.Push(undoAction);
        
        Chart.InPlaceRefresh();
    }
    

    private void Redo()
    {
        if (redoStack.Count == 0) return;
        
        var redoAction = redoStack.Pop();
        redoAction.Redo();
        undoStack.Push(redoAction);
        
        Chart.InPlaceRefresh();
    }
}

public interface IUndoSnapshot
{
    IInstrument parentInstrument { get; }
    void Undo();
    void Redo();
}

public class UndoSnapshot<T> : IUndoSnapshot where T : IEventData
{
    private Dictionary<int, SortedDictionary<int, T>> storedData;
    public IInstrument parentInstrument { get; }
    public void Undo()
    {
        throw new NotImplementedException();
    }

    public void Redo()
    {
        throw new NotImplementedException();
    }

    public void SaveData(LaneSet<T> originationLane)
    {
        storedData[0] = originationLane.ExportData();
    }

    public void SaveData(Lanes<T> originationLanes)
    {
        storedData = originationLanes.ExportData();
    }

    public SortedDictionary<int, T> GetStoredLaneData()
    {
        return storedData[0];
    }

    public Dictionary<int, SortedDictionary<int, T>> GetStoredMultiLaneData()
    {
        return storedData;
    }
    
    public UndoSnapshot(IInstrument originationInstrument)
    {
        this.parentInstrument = originationInstrument;
    }

    public void RestoreSnapshot()
    {
        parentInstrument.PushUndoData(this);
    }
}

public class SyncTrackUndoSnapshot : IUndoSnapshot
{
    public SortedDictionary<int, BPMData> bpmSave { get; }
    public SortedDictionary<int, TSData> tsSave { get; }

    public IInstrument parentInstrument => Chart.SyncTrackInstrument;
    public void Undo()
    {
        throw new NotImplementedException();
    }

    public void Redo()
    {
        throw new NotImplementedException();
    }

    public void RestoreSnapshot()
    {
        parentInstrument.PushUndoData(this);
    }

    public SyncTrackUndoSnapshot(LaneSet<BPMData> tempo, LaneSet<TSData> ts)
    {
        bpmSave = tempo.ExportData();
        tsSave = ts.ExportData();
    }
}
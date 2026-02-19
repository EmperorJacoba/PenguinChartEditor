using System;
using System.Collections.Generic;
using UnityEngine;

public class UndoStack : MonoBehaviour
{
    private Stack<IUndoSnapshot> undoStack;
    private Stack<IUndoSnapshot> redoStack;

    public static UndoStack instance;

    private InputMap inputMap;
    private void Awake()
    {
        instance = this;

        inputMap = new InputMap();
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
        var undoAction = undoStack.Pop();
        undoAction.RestoreSnapshot();
        redoStack.Push(undoAction);
    }

    private void Redo()
    {
        var redoAction = redoStack.Pop();
        redoAction.RestoreSnapshot();
        undoStack.Push(redoAction);
    }
}

public interface IUndoSnapshot
{
    IInstrument originationInstrument { get; }
    void RestoreSnapshot();
}

public class UndoSnapshot<T> : IUndoSnapshot where T : IEventData
{
    private Dictionary<int, SortedDictionary<int, T>> storedData;
    public IInstrument originationInstrument { get; }

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
        this.originationInstrument = originationInstrument;
    }

    public void RestoreSnapshot()
    {
        originationInstrument.PushUndoData(this);
    }
}

public class SyncTrackUndoSnapshot : IUndoSnapshot
{
    private SortedDictionary<int, BPMData> bpmSave;
    private SortedDictionary<int, TSData> tsSave;

    public IInstrument originationInstrument => Chart.SyncTrackInstrument;

    public void RestoreSnapshot()
    {
        originationInstrument.PushUndoData(this);
    }

    public SyncTrackUndoSnapshot(LaneSet<BPMData> tempo, LaneSet<TSData> ts)
    {
        bpmSave = tempo.ExportData();
        tsSave = ts.ExportData();
    }
}
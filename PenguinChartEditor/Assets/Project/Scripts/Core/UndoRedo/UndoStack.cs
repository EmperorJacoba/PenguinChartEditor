using System;
using System.Collections.Generic;
using UnityEngine;

public class UndoStack : MonoBehaviour
{
    private FiniteStack<IUndoSnapshot> undoStack;
    private FiniteStack<IUndoSnapshot> redoStack;

    public static UndoStack instance;

    private void Start()
    {
        instance = this;
        instance.undoStack = new FiniteStack<IUndoSnapshot>(Chart.settings.MaximumSavedUndoActions);
        instance.redoStack = new FiniteStack<IUndoSnapshot>(Chart.settings.MaximumSavedUndoActions);

        Chart.inputMap.Enable();
        Chart.inputMap.StandardCommands.Undo.performed += _ => Undo();
        Chart.inputMap.StandardCommands.Redo.performed += _ => Redo();
    }

    public void PushAction(IUndoSnapshot undoSnapshot)
    {
        undoStack.Push(undoSnapshot);
        redoStack.Clear();
    }

    public void Undo()
    {
        if (undoStack.Count == 0) return;
        
        var undoAction = undoStack.Pop();
        undoAction.Undo();
        redoStack.Push(undoAction);
        
        Chart.InPlaceRefresh();
    }
    
    public void Redo()
    {
        if (redoStack.Count == 0) return;
        
        var redoAction = redoStack.Pop();
        redoAction.Redo();
        undoStack.Push(redoAction);
        
        Chart.InPlaceRefresh();
    }
}
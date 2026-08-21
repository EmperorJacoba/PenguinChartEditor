using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

        Chart.instance.inputMap.Enable();
        Chart.instance.inputMap.StandardCommands.Undo.performed += Undo;
        Chart.instance.inputMap.StandardCommands.Redo.performed += Redo;
    }

    private void OnDestroy()
    {
        Chart.instance.inputMap.StandardCommands.Undo.performed -= Undo;
        Chart.instance.inputMap.StandardCommands.Redo.performed -= Redo;
    }

    public void PushAction(IUndoSnapshot undoSnapshot)
    {
        undoStack.Push(undoSnapshot);
        redoStack.Clear();
    }

    private void Undo(InputAction.CallbackContext _) => Undo();
    public void Undo()
    {
        if (undoStack.Count == 0) return;
        
        var undoAction = undoStack.Pop();
        undoAction.Undo();
        redoStack.Push(undoAction);
        
        Chart.InPlaceRefresh();
    }

    private void Redo(InputAction.CallbackContext _) => Redo();
    public void Redo()
    {
        if (redoStack.Count == 0) return;
        
        var redoAction = redoStack.Pop();
        redoAction.Redo();
        undoStack.Push(redoAction);
        
        Chart.InPlaceRefresh();
    }
}
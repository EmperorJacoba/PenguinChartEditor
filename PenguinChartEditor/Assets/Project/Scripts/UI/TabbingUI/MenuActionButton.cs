using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuActionButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private ActionType actionType;
    [SerializeField] private TopRibbonButton parentRibbonButton;

    public void OnPointerDown(PointerEventData ped)
    {
        parentRibbonButton.DisableContent();
        
        switch (actionType)
        {
            case ActionType.@new:
                Chart.NewFile();
                break;
            case ActionType.open:
                Chart.LoadFile();
                break;
            case ActionType.save:
                Chart.SaveFile();
                break;
            case ActionType.saveAs:
                Chart.SaveFileAs();
                break;
            case ActionType.undo:
                UndoStack.instance.Undo();
                break;
            case ActionType.redo:
                UndoStack.instance.Redo();
                break;
            case ActionType.cut:
                Clipboard.Cut();
                break;
            case ActionType.copy:
                Clipboard.Copy();
                break;
            case ActionType.paste:
                Clipboard.Paste();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    public enum ActionType
    {
        @new,
        open,
        save,
        saveAs,
        undo,
        redo,
        cut,
        copy,
        paste
    }
}
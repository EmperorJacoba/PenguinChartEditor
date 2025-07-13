using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;

public interface IEvent<DataType>
{
    /// <summary>
    /// The tick-time timestamp that this event occurs at.
    /// </summary>
    public int Tick { get; set; }

    /// <summary>
    /// Is this event selected?
    /// </summary>
    public bool Selected { get; set; }

    /// <summary>
    /// The GameObject with the green highlight that displays selection status to the user.
    /// </summary>
    public GameObject SelectionOverlay { get; set; }

    /// <summary>
    /// Is this event currently visible?
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Is the right-click button currently being held down over this event? (for rclick + lclick functionality)
    /// </summary>
    public bool DeletePrimed { get; set; }

    void HandlePointerDown(BaseEventData baseEventData);
    void HandlePointerUp(BaseEventData baseEventData);
    void HandleDragEvent(BaseEventData baseEventData);

    void CopySelection();
    void PasteSelection();
    void DeleteSelection();

    // Get and set functions are required for common abstract functions (ex. copy/paste)
    SortedDictionary<int, DataType> GetEvents();
    void SetEvents(SortedDictionary<int, DataType> newEvents);

}

[RequireComponent(typeof(EventTrigger))]
public abstract class Event<T> : MonoBehaviour, IEvent<T> where T : IEventData
{
    protected InputMap inputMap;
    public int Tick { get; set; }
    public abstract SortedDictionary<int, T> GetEvents();
    public abstract void HandleDragEvent(BaseEventData baseEventData);
    public abstract void SetEvents(SortedDictionary<int, T> newEvents);
    [field: SerializeField] public GameObject SelectionOverlay { get; set; }
    public bool DeletePrimed { get; set; } // future: make global across events 

    public void CopySelection()
    {
        var copyAction = new Copy<T>(GetEvents());
        copyAction.Execute();
    }

    public virtual void PasteSelection()
    {
        var pasteAction = new Paste<T>(GetEvents());
        pasteAction.Execute(BeatlinePreviewer.currentPreviewTick);
        TempoManager.UpdateBeatlines();

        // paste currently crashes when paste zone exceeds the screen - fix
        // implement other event actions!!
    }

    public virtual void CutSelection()
    {
        var cutAction = new Cut<T>(GetEvents());
        cutAction.Execute();
    }

    public virtual void DeleteSelection()
    {
        var deleteAction = new Delete<T>(GetEvents());
        deleteAction.Execute();
        TempoManager.UpdateBeatlines();
    }

    public virtual void CreateEvent(int newTick, T newData)
    {
        var createAction = new Create<T>(GetEvents());
        createAction.Execute(newTick, newData);
        TempoManager.UpdateBeatlines();
    }

    public bool Selected
    {
        get
        {
            return _selected;
        }
        set
        {
            SelectionOverlay.SetActive(value);
            _selected = value;
        }
    }
    bool _selected = false;

    public bool CheckForSelection()
    {
        if (CheckIfDataPresentInSelection(GetEvents())) return true;
        else return false;
    }

    public static int lastTickSelection;
    public void CalculateSelectionStatus(PointerEventData.InputButton clickButton)
    {
        var targetEventSet = GetEvents();

        // Goal is to follow standard selection functionality of most productivity programs

        if (clickButton != PointerEventData.InputButton.Left) return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            SelectionManager.selection.Clear();

            var minNum = Math.Min(lastTickSelection, Tick);
            var maxNum = Math.Max(lastTickSelection, Tick);

            var selection = targetEventSet.Where(x => x.Key <= maxNum && x.Key >= minNum);

            foreach (var @event in selection)
            {
                SelectionManager.selection.Add(@event.Key, new() { @event.Value });
            }
        }
        else if (Input.GetKey(KeyCode.LeftControl) && CheckIfDataPresentInSelection(targetEventSet))
        {
            SelectionManager.selection[Tick].Remove(targetEventSet[Tick]);
            return;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            SelectionManager.selection[Tick].Add(targetEventSet[Tick]);
        }
        else
        {
            SelectionManager.selection.Clear();
            SelectionManager.selection.Add(Tick, new() { targetEventSet[Tick] });
        }
    }

    bool CheckIfDataPresentInSelection(SortedDictionary<int, T> targetEventSet)
    {
        if (!SelectionManager.selection.ContainsKey(Tick)) return false;
        else if (SelectionManager.selection[Tick].Contains(targetEventSet[Tick])) return true;
        return false;
    }

    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            gameObject.SetActive(value);
        }
    }

    public void HandlePointerDown(BaseEventData baseEventData)
    {
        var pointerData = (PointerEventData)baseEventData;
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = true;
        }
    }

    public void HandlePointerUp(BaseEventData baseEventData)
    {
        var pointerData = (PointerEventData)baseEventData;
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = false;
        }
    }
}

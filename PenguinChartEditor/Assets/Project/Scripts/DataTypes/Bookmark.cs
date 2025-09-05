using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Bookmark : Label<BookmarkData>
{
    public static EventData<BookmarkData> EventData = new();
    public override EventData<BookmarkData> GetEventData() => EventData;

    static MoveData<BookmarkData> moveData = new();
    public override MoveData<BookmarkData> GetMoveData() => moveData;

    public override void SetEvents(SortedDictionary<int, BookmarkData> newEvents)
    {
        if (!EventData.Events.ContainsKey(0))
        {
            EventData.Events.Add(0, new BookmarkData(moveData.currentMoveAction.poppedData[0].Name));
        }
        EventData.Events = newEvents;
    }

    public override string ConvertDataToPreviewString()
    {
        return EventData.Events[Tick].Name;
    }

    public override void HandleManualEndEdit(string newVal)
    {
        EventData.Events[Tick] = new BookmarkData(newVal);

        ConcludeManualEdit();
    }
}
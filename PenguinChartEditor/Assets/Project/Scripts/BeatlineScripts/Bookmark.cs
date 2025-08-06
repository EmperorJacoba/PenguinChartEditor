using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Bookmark : Label<BookmarkData>
{
    public static EventData<BookmarkData> EventData = new();
    public override EventData<BookmarkData> GetEventData() => EventData;

    public override void SetEvents(SortedDictionary<int, BookmarkData> newEvents)
    {
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

    public override void OnDrag(PointerEventData baseEventData)
    {
        
    }
}
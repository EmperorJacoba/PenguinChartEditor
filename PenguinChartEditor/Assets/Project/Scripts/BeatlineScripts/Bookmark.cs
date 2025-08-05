using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Bookmark : Label<BookmarkData>
{
    // Move events to larger local section class
    public static SortedDictionary<int, BookmarkData> Events { get; set; } = new();

    public override SortedDictionary<int, BookmarkData> GetEvents()
    {
        return Events;
    }

    public override void SetEvents(SortedDictionary<int, BookmarkData> newEvents)
    {
        Events = newEvents;
    }

    public static HashSet<int> SelectedBookmarkEvents { get; set; } = new();
    public override HashSet<int> GetSelectedEvents()
    {
        return SelectedBookmarkEvents;
    }

    SortedDictionary<int, BookmarkData> BookmarkClipboard = new();
    public override SortedDictionary<int, BookmarkData> GetEventClipboard()
    {
        return BookmarkClipboard;
    }

    public override string ConvertDataToPreviewString()
    {
        return Events[Tick].Name;
    }

    public override void HandleManualEndEdit(string newVal)
    {
        Events[Tick] = new BookmarkData(newVal);

        ConcludeManualEdit();
    }

    public override void HandleDragEvent(BaseEventData baseEventData)
    {
        
    }
}
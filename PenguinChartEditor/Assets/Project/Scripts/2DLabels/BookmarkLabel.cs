using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/*
public class BookmarkLabel : Label<BookmarkData>
{
    public static EventData<BookmarkData> EventData = new();
    public override EventData<BookmarkData> GetEventData() => EventData;
    public override SortedDictionary<int, BookmarkData> GetEventSet() => Bookmark.Events;

    static MoveData<BookmarkData> moveData = new();
    public override MoveData<BookmarkData> GetMoveData() => moveData;

    public override void SetEvents(SortedDictionary<int, BookmarkData> newEvents)
    {
        if (!Bookmark.Events.ContainsKey(0))
        {
            Bookmark.Events.Add(0, new BookmarkData(moveData.currentMoveAction.poppedData[0].Name));
        }
        Bookmark.Events = newEvents;
    }

    public override string ConvertDataToPreviewString()
    {
        return Bookmark.Events[Tick].Name;
    }

    public override void HandleManualEndEdit(string newVal)
    {
        Bookmark.Events[Tick] = new BookmarkData(newVal);

        ConcludeManualEdit();
    }
} */
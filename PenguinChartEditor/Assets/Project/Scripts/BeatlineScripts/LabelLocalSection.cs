using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class LabelLocalSection : Label<SectionData>
{
    // Move events to larger local section class
    public static SortedDictionary<int, SectionData> Events { get; set; } = new();

    public override SortedDictionary<int, SectionData> GetEvents()
    {
        return Events;
    }

    public override void SetEvents(SortedDictionary<int, SectionData> newEvents)
    {
        Events = newEvents;
    }

    public static HashSet<int> SelectedSectionEvents { get; set; } = new();
    public override HashSet<int> GetSelectedEvents()
    {
        return SelectedSectionEvents;
    }

    SortedDictionary<int, SectionData> SectionClipboard = new();
    public override SortedDictionary<int, SectionData> GetEventClipboard()
    {
        return SectionClipboard;
    }

    public override string ConvertDataToPreviewString()
    {
        return Events[Tick].Name;
    }

    public override void HandleManualEndEdit(string newVal)
    {
        Events[Tick] = new SectionData(newVal, true);

        ConcludeManualEdit();
    }

    public override void HandleDragEvent(BaseEventData baseEventData)
    {
        
    }
}
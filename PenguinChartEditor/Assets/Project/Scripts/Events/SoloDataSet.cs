using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SoloDataSet
{
    public LaneSet<SoloEventData> SoloEvents;
    public SelectionSet<SoloEventData> SelectedStartEvents;

    /// <summary>
    /// Note: SelectedEndEvents uses the StartTick property as the ID for continuity with SoloEvents.
    /// <para>Example: SoloData: StartTick = 192, EndTick = 4800 ||
    /// SelectedStartEvents = { 192 } ||
    /// SelectedEndEvents = { 192 }</para>
    /// Both point to the same event, but SelectedEndEvents really represents 4800.
    /// </summary>
    public SelectionSet<SoloEventData> SelectedEndEvents;
    
    public SoloDataSet()
    {
        SoloEvents = new LaneSet<SoloEventData>();
        SelectedStartEvents = new SelectionSet<SoloEventData>(SoloEvents);
        SelectedEndEvents = new SelectionSet<SoloEventData>(SoloEvents);
    }

    public void DeleteSelection()
    {
        SelectedStartEvents.PopSelectedTicksFromLane();
        var eventsToCorrect = SelectedEndEvents.PopSelectedTicksFromLane();
        foreach (var @event in eventsToCorrect)
        {
            var nextSoloEvent = Chart.LoadedInstrument.SoloData.SoloEvents.Where(x => x.Value.StartTick > @event.Value.EndTick);

            var endTick = SongTime.SongLengthTicks - @event.Value.StartTick;
            if (nextSoloEvent.Count() > 0) endTick = nextSoloEvent.Min(x => x.Value.StartTick) - (Chart.Resolution / (DivisionChanger.CurrentDivision / 4));

            SoloEvents.Add(@event.Key, new SoloEventData(@event.Value.StartTick, endTick));
        }
    }

    public void DeleteTick(int eventTick)
    {
        var startCandidates = GetStartSoloAtTick(eventTick);
        var endCandidates = GetEndSoloAtTick(eventTick);

        foreach (var @event in startCandidates)
        {
            SoloEvents.Remove(@event);
        }

        foreach (var @event in endCandidates)
        {
            SoloEvents.Remove(@event);
            SoloEvents.Add(@event.Key, new SoloEventData(@event.Value.StartTick, SongTime.SongLengthTicks - @event.Value.StartTick));
        }
    }

    public List<KeyValuePair<int, SoloEventData>> GetAnySoloMarkerAtTick(int eventTick) => SoloEvents.Where(x => x.Value.StartTick == eventTick || x.Value.EndTick == eventTick).ToList();
    public List<KeyValuePair<int, SoloEventData>> GetStartSoloAtTick(int eventTick) => SoloEvents.Where(x => x.Value.StartTick == eventTick).ToList();
    public List<KeyValuePair<int, SoloEventData>> GetEndSoloAtTick(int eventTick) => SoloEvents.Where(x => x.Value.EndTick == eventTick).ToList();

    public void ClearSelection()
    {
        SelectedStartEvents.Clear();
        SelectedEndEvents.Clear();
    }

    public void SelectTick(int eventTick)
    {
        var startCandidates = GetStartSoloAtTick(eventTick);
        var endCandidates = GetEndSoloAtTick(eventTick);

        foreach (var @event in startCandidates)
        {
            SelectedStartEvents.Add(@event.Key);
        }

        foreach (var @event in endCandidates)
        {
            SelectedEndEvents.Add(@event.Key);
        }
    }

    public void SelectTicksInRange(int startTick, int endTick)
    {
        var startCandidates = SoloEvents.Where(x => x.Value.StartTick >= startTick && x.Value.StartTick <= endTick).ToList();
        var endCandidates = SoloEvents.Where(x => x.Value.EndTick >= startTick && x.Value.EndTick <= endTick).ToList();

        foreach (var @event in startCandidates)
        {
            SelectedStartEvents.Add(@event.Key);
        }

        foreach (var @event in endCandidates)
        {
            SelectedEndEvents.Add(@event.Key);
        }
    }

    public void RemoveTickFromAllSelections(int tick)
    {
        SelectedStartEvents.Remove(tick);
        SelectedEndEvents.Remove(tick);
    }
}
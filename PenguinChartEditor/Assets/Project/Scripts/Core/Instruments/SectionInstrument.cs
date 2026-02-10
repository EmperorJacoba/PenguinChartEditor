using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class SectionInstrument : BaseInstrument<SectionData>
{
    #region Data Setup

    public LaneSet<SectionData> GetLaneData() => laneData;
    private readonly LaneSet<SectionData> laneData;

    public SelectionSet<SectionData> GetLaneSelection() => selection;
    private readonly SelectionSet<SectionData> selection;
    public override ISelection GetLaneSelection(int lane) => selection;
    
    public override List<int> GetUniqueTickSet() => laneData.Keys.ToList();

    #endregion

    #region Selections
    
    public override bool NoteSelectionContains(int tick, int lane) => selection.Contains(tick);
    public override int NoteSelectionCount => selection.Count();
    public override bool IsNoteSelectionEmpty() => selection.Count == 0;

    #region Internal Implementations

    protected override void InternalSelectAll() => selection.SelectAllInLane();
    protected override void InternalClearAllSelections() => selection.Clear();
    protected override void InternalShiftClickSelectLane(int start, int end, int lane) => selection.ShiftClickSelectInRange(start, end);
    protected override void InternalShiftClickSelect(int start, int end) => selection.ShiftClickSelectInRange(start, end);
    protected override void InternalClearTickFromAllSelections(int tick) => selection.Remove(tick);
    protected override void InternalDeleteTicksInSelection() => selection.PopSelectedTicksFromLane();
    protected override void InternalDeleteSelection() => selection.PopSelectedTicksFromLane();

    #endregion
    
    public void SetSectionSelectionName(string newName)
    {
        if (newName == MULTIPLE_SELECTION_WARNING) return;
        
        foreach (var section in selection)
        {
            laneData[section] = new SectionData(newName);
        }
        
        Chart.InPlaceRefresh();
    }

    private const string MULTIPLE_SELECTION_WARNING = "[[Multiple Sections Selected]]";
    public string GetSelectedSectionName()
    {
        string sectionName = null;
        
        // Allows for editing the names of multiple sections, while also warning about it.
        foreach (var section in selection)
        {
            if (!selection.TryGetSelectedItem(section, out var sectionData))
            {
                continue;
            }
            
            if (sectionName == null)
            {
                sectionName = sectionData.Name;
                continue;
            }

            if (sectionName != sectionData.Name)
            {
                return MULTIPLE_SELECTION_WARNING;
            }
        }

        return sectionName;
    }
    
    #endregion

    #region Add/Delete

    protected override void InternalDeleteTickInLane(int tick, int lane) => laneData.Remove(tick);
    protected override void InternalDeleteAllEventsAtTick(int tick) => laneData.Remove(tick);

    #endregion

    #region Constructor

    public SectionInstrument(List<KeyValuePair<int, string>> events)
    {
        laneData = new LaneSet<SectionData>();
        selection = new SelectionSet<SectionData>(laneData);
        
        AddChartFormattedEventsToInstrument(events);
    }
    
    #endregion

    #region Moving

    protected override bool InternalMoveSelection() => mover.Move1DSelection(this, laneData, selection);

    // No extra post-move actions needed.
    protected override void InternalCompleteMove() {}

    #endregion

    #region Import

    public override void AddChartFormattedEventsToInstrument(string clipboardData, int offset)
    {
        throw new System.NotImplementedException();
    }

    private void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> events)
    {
        foreach (var @event in events)
        {
            var splitEvent = @event.Value.Split(" ", 2);

            if (splitEvent.Length != 2)
            {
                Debug.Log($"Could not split section event properly. {@event.Key} = {@event.Value}");
                continue;
            }

            if (splitEvent[0] != "E")
            {
                Debug.Log($"Invalid/unrecognized event type identified as a section. {splitEvent[0]}");
                continue;
            }

            var sectionEvent = splitEvent[1];
            
            // Change this to check for escape characters if any game ever supports quotations in a section/lyric.
            sectionEvent = sectionEvent.Replace("\"", "");

            var splitSection = sectionEvent.Split(" ", 2);

            if (
                splitSection.Length != 2 || splitSection[0] != "section"
            )
            {
                splitSection = sectionEvent.Split("_", 2);

                if (splitSection.Length != 2 || !(splitSection[0] != "section" || splitSection[0] != "prc"))
                {
                    Debug.Log($"Invalid separator char between section identifier and section name. {@event.Key} = {@event.Value}");
                    continue;
                }
            }
            
            laneData.Add(@event.Key, new SectionData(splitSection[1]));
        }
    }

    #endregion

    #region Export

    public override string ConvertSelectionToString()
    {
        throw new System.NotImplementedException();
    }
    
    public override List<string> ExportAllEvents()
    {
        throw new System.NotImplementedException();
    }

    #endregion

    #region Not Implemented

    public override SoloDataSet SoloData
    {
        get => null;
        set {}
    }
    public override ILaneData GetBarLaneData() => throw new System.NotImplementedException("No bar lane in sections");
    public override ILaneData GetLaneData(int lane) => laneData;
    protected override void InternalSetSelectionToNewLane(int destinationLane)
    {
        throw new NotImplementedException("No cross-lane selections in this instrument.");
    }

    #endregion
}
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;

public sealed class SectionInstrument : BaseInstrument<SectionData>
{
    #region Data Setup

    protected override IMultiLaneController LaneController => Lanes;
    private Lanes<SectionData> Lanes;

    public LaneSet<SectionData> GetLaneData() => Lanes.GetLane(0);
    public SelectionSet<SectionData> GetLaneSelection() => Lanes.GetLaneSelection(0);

    #endregion
    
    public void SetSectionSelectionName(string newName)
    {
        if (newName == MULTIPLE_SELECTION_WARNING) return;
        
        foreach (var section in GetLaneSelection())
        {
            GetLaneData()[section] = new SectionData(newName);
        }
        
        Chart.InPlaceRefresh();
    }

    private const string MULTIPLE_SELECTION_WARNING = "[[Multiple Sections Selected]]";
    public string GetSelectedSectionName()
    {
        string sectionName = null;
        var selection = GetLaneSelection();
        
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
    
    #region Constructor

    private void InitializeReferences()
    {
        InstrumentID = HeaderType.Events;
        Lanes = new Lanes<SectionData>(1);
    }

    public SectionInstrument(List<KeyValuePair<int, string>> events)
    {
        InitializeReferences();
        AddChartFormattedEventsToInstrument(events);
    }

    public SectionInstrument(List<PenguinEventSection> lanes)
    {
        InstrumentName = InstrumentType.events;
        Difficulty = DifficultyType.easy;
        Lanes = new Lanes<SectionData>(lanes, new List<int> {0});
    }

    public SectionInstrument()
    {
        InitializeReferences();   
    }
    
    #endregion

    #region Import

    protected override void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> events)
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
            sectionEvent = sectionEvent.Replace("\"", "").Trim();

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
            
            GetLaneData().Add(@event.Key, new SectionData(splitSection[1]));
        }
    }

    #endregion

    #region Export

    public override string ConvertSelectionToString()
    {
        StringBuilder notes = new();
        var exportedSelection = GetLaneSelection().ExportNormalizedData();
        
        foreach (var note in exportedSelection)
        {
            var valueString = $"\t{note.Key} = {note.Value.ToChartFormat(0)[0]}";
            notes.AppendLine(valueString);
        }

        return notes.ToString();
    }

    public override IInstrument DuplicateToNewInstrument(HeaderType newInstrumentID)
    {
        throw new ArgumentException("SectionInstruments cannot be duplicated in the same chart file.");
    }

    #endregion

    #region Not Implemented

    public override SoloDataSet SoloData
    {
        get => null;
        set {}
    }
    public override ILaneData GetBarLaneData() => throw new System.NotImplementedException("No bar lane in sections");
    protected override void InternalSetSelectionToNewLane(int destinationLane)
    {
        throw new NotImplementedException("No cross-lane selections in this instrument.");
    }

    #endregion
}
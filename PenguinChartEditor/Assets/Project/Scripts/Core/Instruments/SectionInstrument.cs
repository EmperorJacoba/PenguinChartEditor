using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class SectionInstrument : IInstrument
{
    #region Attributes

    public InstrumentType InstrumentName { get; set; } = InstrumentType.events;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;
    public HeaderType InstrumentID { get; } = HeaderType.Events;
    
    #endregion

    #region Data Setup

    public LaneSet<SectionData> GetLaneData() => laneData;
    private LaneSet<SectionData> laneData;

    public SelectionSet<SectionData> GetLaneSelection() => selection;
    private SelectionSet<SectionData> selection;
    
    public List<int> GetUniqueTickSet() => laneData.Keys.ToList();

    #endregion

    #region Selections

    public void ClearAllSelections() => selection.Clear();
    public bool NoteSelectionContains(int tick, int lane) => selection.Contains(tick);
    public int NoteSelectionCount => selection.Count();
    public void ShiftClickSelectLane(int start, int end, int lane) => selection.ShiftClickSelectInRange(start, end);
    public void ShiftClickSelect(int start, int end) => selection.ShiftClickSelectInRange(start, end);
    public void ShiftClickSelect(int tick) => selection.ShiftClickSelectInRange(tick, tick);
    public void ClearTickFromAllSelections(int tick) => selection.Remove(tick);
    public void DeleteTicksInSelection() => selection.PopSelectedTicksFromLane();
    public bool IsNoteSelectionEmpty() => selection.Count == 0;
    private void DeleteSelection() => selection.PopSelectedTicksFromLane();
    public ISelection GetLaneSelection(int lane) => selection;
    
    private void CheckForSelectionClear()
    {
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || Chart.instance.SceneDetails.IsEventDataHit()) return;
        
        ClearAllSelections();
        Chart.InPlaceRefresh();
    }
    
    #endregion

    #region Add/Delete

    public void DeleteTickInLane(int tick, int lane) => laneData.Remove(tick);
    public void DeleteAllEventsAtTick(int tick) => laneData.Remove(tick);

    #endregion

    #region Constructor

    public SectionInstrument(List<KeyValuePair<int, string>> events)
    {
        laneData = new LaneSet<SectionData>();
        selection = new SelectionSet<SectionData>(laneData);
        mover = new MoveHelper<SectionData>();
        
        AddChartFormattedEventsToInstrument(events);
    }
    
    private InputMap inputMap;
    public void SetUpInputMap()
    {
        inputMap = new InputMap();
        inputMap.Enable();

        inputMap.Charting.XYDrag.performed += x => MoveSelection();
        inputMap.Charting.LMB.canceled += x => CompleteMove();
        inputMap.Charting.Delete.performed += x => DeleteSelection();
        inputMap.Charting.LMB.performed += x => CheckForSelectionClear();
        inputMap.Charting.SelectAll.performed += x => selection.SelectAllInLane();
        inputMap.Charting.ClearSelection.performed += x => ClearAllSelections();
    }
    
    #endregion

    #region Moving

    private MoveHelper<SectionData> mover;

    private void MoveSelection()
    {
        if (mover.Move1DSelection(this, laneData, selection))
        {
            Chart.InPlaceRefresh();
        }
    }

    private void CompleteMove()
    {
        mover.Reset();
    }

    #endregion

    #region Import

    public void AddChartFormattedEventsToInstrument(string clipboardData, int offset)
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

    public string ConvertSelectionToString()
    {
        throw new System.NotImplementedException();
    }
    
    public List<string> ExportAllEvents()
    {
        throw new System.NotImplementedException();
    }

    #endregion

    #region Not Implemented

    public SoloDataSet SoloData
    {
        get { throw new NotImplementedException("Sections do not have solo events. If you are using the SoloEvent suite, it is not required."); }
        set { throw new NotImplementedException("Sections do not have solo events. If you are using the SoloEvent suite, it is not required."); }
    }
    public ILaneData GetBarLaneData() => throw new System.NotImplementedException("No bar lane in sections");
    public ILaneData GetLaneData(int lane) => laneData;
    
    #endregion
}
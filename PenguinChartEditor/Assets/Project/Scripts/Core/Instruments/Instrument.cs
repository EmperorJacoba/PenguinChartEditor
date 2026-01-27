using System.Collections.Generic;
using UnityEngine.EventSystems;

// this is untyped because these methods (by design) apply regardless of type. Specify type in calls to an instrument
// if necessary.
public interface IInstrument
{
    public const int SOLO_DATA_LANE_ID = int.MaxValue;
    SoloDataSet SoloData { get; set; }
    InstrumentType InstrumentName { get; set; }
    DifficultyType Difficulty { get; set; }
    HeaderType InstrumentID { get; }
    List<string> ExportAllEvents();

    void ClearAllSelections();
    bool NoteSelectionContains(int tick, int lane);
    int NoteSelectionCount { get; }
    public void ShiftClickSelectLane(int start, int end, int lane);
    public void ShiftClickSelect(int start, int end);
    public void ShiftClickSelect(int tick);
    public void ClearTickFromAllSelections(int tick);

    List<int> GetUniqueTickSet();
    
    /// <remarks>
    /// Call from the main thread, because this uses an InputActionMap. Do not call in a constructor!
    /// </remarks>
    void SetUpInputMap();

    string ConvertSelectionToString();
    void AddChartFormattedEventsToInstrument(string clipboardData, int offset);

    void DeleteTicksInSelection();
    void DeleteTickInLane(int tick, int lane);
    void DeleteAllEventsAtTick(int tick);

    public ILaneData GetLaneData(int lane);
    
    /// <remarks>
    /// A "bar lane" refers to a lane that a note receiver prefab must respond to in a non-exclusively
    /// fretted view, even if its lane data is not the bar lane data. See: open notes, kick notes.
    /// </remarks>
    public ILaneData GetBarLaneData();
    public ISelection GetLaneSelection(int lane);
    bool IsNoteSelectionEmpty();
}

public interface ISustainableInstrument
{
    public void ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event);
    public int CalculateSustainClamp(int sustainLength, int tick, int lane);
    public void SetSelectionSustain(int ticks);
    public void SetSelectionSustain(float bars);
}


using System.Collections.Generic;
using UnityEngine.EventSystems;

// please please please do not add <T> to this
// i have done this like 7 times and it makes chartparser really ugly
// future me: PLEASE STOP ADDING <T>! IT WILL NOT WORK THIS TIME! LIKE THE 7 OTHER TIMES
public interface IInstrument
{
    SoloDataSet SoloData { get; set; }
    InstrumentType InstrumentName { get; set; }
    DifficultyType Difficulty { get; set; }
    HeaderType InstrumentID { get; }
    List<string> ExportAllEvents();

    void ClearAllSelections();
    bool NoteSelectionContains(int tick, int lane);
    int NoteSelectionCount { get; }
    public void ShiftClickSelect(int start, int end);
    public void ShiftClickSelect(int tick);
    public void ClearTickFromAllSelections(int tick);

    List<int> GetUniqueTickSet();
    void SetUpInputMap();

    string ConvertSelectionToString();
    void AddChartFormattedEventsToInstrument(string lines, int offset);
    void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines);

    void DeleteTicksInSelection();
    void DeleteTickInLane(int tick, int lane);
    void DeleteAllEventsAtTick(int tick);

    public ILaneData GetLaneData(int lane);
    public ILaneData GetBarLaneData();
    public ISelection GetLaneSelection(int lane);
}

public interface ISustainableInstrument
{
    public void ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event);
}


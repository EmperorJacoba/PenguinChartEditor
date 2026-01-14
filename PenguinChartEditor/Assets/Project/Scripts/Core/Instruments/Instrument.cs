using System.Collections.Generic;
using UnityEngine.EventSystems;

// please please please do not add <T> to this
// i have done this like 7 times and it makes chartparser really ugly
// future me: PLEASE STOP ADDING <T>! IT WILL NOT WORK THIS TIME! LIKE THE 7 OTHER TIMES
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
    void SetUpInputMap();

    string ConvertSelectionToString();
    void AddChartFormattedEventsToInstrument(string clipboardData, int offset);

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
    public int CalculateSustainClamp(int sustainLength, int tick, int lane);
}


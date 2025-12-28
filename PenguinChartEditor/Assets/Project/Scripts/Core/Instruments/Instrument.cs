using System.Collections.Generic;

// please please please do not add <T> to this
// i have done this like 7 times and it makes chartparser really ugly
// future me: PLEASE STOP ADDING <T>! IT WILL NOT WORK THIS TIME! LIKE THE 7 OTHER TIMES
public interface IInstrument
{
    /// <summary>
    /// This is a non-traditional lane. Access solo events via their start tick; all other lane set rules apply as normal
    /// </summary>
    LaneSet<SoloEventData> SoloEvents { get; set; }
    SelectionSet<SoloEventData> SoloEventSelection { get; set; }
    InstrumentType InstrumentName { get; set; }
    DifficultyType Difficulty { get; set; }
    List<string> ExportAllEvents();

    void ClearAllSelections();
    bool SelectionContains(int tick, int lane);
    int NoteSelectionCount { get; }
    public void ShiftClickSelect(int start, int end);
    public void ShiftClickSelect(int tick);
    public void ShiftClickSelect(int tick, bool temporary);
    public void ReleaseTemporaryTicks();
    public void RemoveTickFromAllSelections(int tick);

    List<int> UniqueTicks { get; }

    void SetUpInputMap();

    string ConvertSelectionToString();
    void AddChartFormattedEventsToInstrument(string lines, int offset);
    void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines);

    void DeleteTicksInSelection();
    void DeleteTick(int tick, int lane);
    void DeleteAllEventsAtTick(int tick);
       
}


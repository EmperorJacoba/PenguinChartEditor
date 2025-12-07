using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class SyncTrackInstrument : IInstrument
{
    const int DEFAULT_TS_DENOMINATOR = 4;
    const string TEMPO_EVENT_INDICATOR = "B";
    const string TIME_SIGNATURE_EVENT_INDICATOR = "TS";
    const string ANCHOR_INDICATOR = "A";
    const string SYNC_TRACK_ERROR = "[SyncTrack] has invalid tempo event:";
    const float BPM_FORMAT_CONVERSION = 1000.0f;
    const int TS_POWER_CONVERSION_NUMBER = 2;
    const float SECONDS_PER_MINUTE = 60;


    // Lanes located in respective libraries
    // This class is pretty much for shift click and clearing both TS and BPMData selections when needed
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }

    public SyncTrackInstrument()
    {
        bpmSelection = new(Tempo.Events);
        tsSelection = new(TimeSignature.Events);

        // Tempo.Events.UpdateNeededAtTick += modifiedTick => Tempo.RecalculateTempoEventDictionary(modifiedTick);
    }


    public SelectionSet<BPMData> bpmSelection;
    public SelectionSet<TSData> tsSelection;

    public MoveData<BPMData> bpmMoveData = new();
    public MoveData<TSData> tsMoveData = new();

    public InstrumentType Instrument { get; set; } = InstrumentType.synctrack;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;

    public int TotalSelectionCount
    {
        get
        {
            return bpmSelection.Count + tsSelection.Count;
        }
    }

    public List<int> UniqueTicks
    {
        get
        {
            var hashSet = Tempo.Events.ExportData().Keys.ToHashSet();
            hashSet.UnionWith(TimeSignature.Events.ExportData().Keys.ToHashSet());
            List<int> list = new(hashSet);
            list.Sort();
            return list;
        }
    }


    public void ClearAllSelections()
    {
        bpmSelection.Clear();
        tsSelection.Clear();
    }

    public void ShiftClickSelect(int start, int end)
    {
        bpmSelection.Clear();
        tsSelection.Clear();

        bpmSelection.AddInRange(start, end);
        tsSelection.AddInRange(start, end);
    }

    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);
    public List<string> ExportAllEvents()
    {
        throw new NotImplementedException("Use export functions in Tempo and TimeSignature libraries.");
        // maybe use this instead of individual libraries in future?
    }

    public void ShiftClickSelect(int tick, bool temporary) => ShiftClickSelect(tick);
    public void ReleaseTemporaryTicks() { } // unneeded - no sustains lol

    public void RemoveTickFromAllSelections(int tick)
    {
        bpmSelection.Remove(tick);
        tsSelection.Remove(tick);
    } // unneeded

    public void SetUpInputMap() { }

    public string ConvertSelectionToString()
    {
        var bpmSelectionData = bpmSelection.ExportNormalizedData();
        var tsSelectionData = tsSelection.ExportNormalizedData();
        var stringIDs = new List<KeyValuePair<int, string>>();

        foreach (var item in bpmSelectionData)
        {
            stringIDs.Add(
                new(item.Key, item.Value.ToChartFormat(0))
                );
            if (item.Value.Anchor)
            {
                stringIDs.Add(
                    new(item.Key, $"A {item.Value.Timestamp * Tempo.MICROSECOND_CONVERSION}")
                    );
            }
        }
        foreach (var item in tsSelectionData)
        {
            stringIDs.Add(
                new(item.Key, item.Value.ToChartFormat(0))
                );
        }

        stringIDs.Sort((a, b) => a.Key.CompareTo(b.Key));

        StringBuilder combinedIDs = new();

        foreach (var id in stringIDs)
        {
            combinedIDs.AppendLine($"\t{id.Key} = {id.Value}");
        }
        return combinedIDs.ToString();
    }

    public void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines)
    {
        HashSet<int> anchoredTicks = new(); // allows for versitility if A event comes before or after tempo event proper
        int recalcTick = int.MaxValue;

        foreach (var entry in lines)
        {
            if (entry.Value.Contains(TEMPO_EVENT_INDICATOR))
            {
                var eventData = entry.Value;
                eventData = eventData.Replace($"{TEMPO_EVENT_INDICATOR} ", ""); // SPACE IS VERY IMPORTANT HERE

                if (!int.TryParse(eventData, out int bpmNoDecimal))
                {
                    Chart.Log($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid tempo entry.");
                    continue;
                }

                float bpmWithDecimal = bpmNoDecimal / BPM_FORMAT_CONVERSION;

                Tempo.Events[entry.Key] = new((float)Math.Round(bpmWithDecimal, 3), 0, anchoredTicks.Contains(entry.Key));
                if (recalcTick > entry.Key) recalcTick = entry.Key;
            }
            else if (entry.Value.Contains(TIME_SIGNATURE_EVENT_INDICATOR))
            {
                var eventData = entry.Value;
                eventData = eventData.Replace($"{TIME_SIGNATURE_EVENT_INDICATOR} ", "");

                string[] tsParts = eventData.Split(" ");

                if (!int.TryParse(tsParts[0], out int numerator))
                {
                    Chart.Log($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature numerator.");
                    continue;
                }

                int denominator = DEFAULT_TS_DENOMINATOR;
                if (tsParts.Length == 2) // There is no space in the event value (only one number)
                {
                    if (!int.TryParse(tsParts[1], out int denominatorLog2))
                    {
                        Chart.Log($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature denominator.");
                        continue;
                    }
                    denominator = (int)Math.Pow(TS_POWER_CONVERSION_NUMBER, denominatorLog2);
                }

                TimeSignature.Events[entry.Key] = new TSData(numerator, denominator);
            }
            else if (entry.Value.Contains(ANCHOR_INDICATOR))
            {
                if (Tempo.Events.Contains(entry.Key))
                {
                    Tempo.Events[entry.Key] = new(Tempo.Events[entry.Key].BPMChange, Tempo.Events[entry.Key].Timestamp, true);
                }
                else
                {
                    anchoredTicks.Add(entry.Key);
                }

                // if for some reason you need to add parsing for the microsecond value do it here
                // that is not here because a) penguin already works with and calculates the timestamps of every event
                // and b) if the microsecond value is parsed and it's not aligned with the Format calculations,
                // then what is penguin supposed to do? change the incoming BPM data? no
                // I think it has the microsecond value for programs that choose not to work with timestamps
                // (timestamps are easier to deal with in my opinion, even if an extra (minor) step is needed after every edit)
            }
        }

        Tempo.RecalculateTempoEventDictionary();
        Chart.Refresh();
    }

    public void AddChartFormattedEventsToInstrument(string clipboardData, int offset)
    {
        var lines = new List<KeyValuePair<int, string>>();
        var clipboardAsLines = clipboardData.Split("\n");
        foreach (var line in clipboardAsLines)
        {
            var parts = line.Split(" = ", 2);
            if (!int.TryParse(parts[0].Trim(), out int tick))
            {
                Chart.Log($"Problem parsing tick {parts[0].Trim()}");
                continue;
            }

            lines.Add(new(tick + offset, parts[1]));
        }
        AddChartFormattedEventsToInstrument(lines);
    }

    public void DeleteTicksInSelection()
    {
        bpmSelection.PopSelectedTicksFromLane();
        tsSelection.PopSelectedTicksFromLane();

        Tempo.RecalculateTempoEventDictionary();
    }
}

/*
public class VoxInstrument : IInstrument
{
    // not yet implemented
} */
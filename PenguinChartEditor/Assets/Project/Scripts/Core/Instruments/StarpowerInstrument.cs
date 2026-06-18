using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Notes: The equivilent of LaneOrientation in this instrument is HeaderType - as each instrument track has independent starpower.
/// </summary>
public class StarpowerInstrument : BaseSustainableInstrument<StarpowerEventData>
{
    #region Constants

    private const int EVENT_TYPE_IDENTIFIER_INDEX = 1;
    private const int SUSTAIN_INDEX = 2;
    private const string DRUM_FILL_ID = "64";
    private const string STARPOWER_ID = "2";

    #endregion

    #region Data Access

    protected override IMultiLaneController LaneController => Lanes;

    /// <summary>
    /// Access instrument data with GetLane(int), where int is casted version of HeaderType,
    /// since each traditional instrument has its own set of starpower events.
    /// </summary>
    private Lanes<StarpowerEventData> Lanes;
    public LaneSet<StarpowerEventData> GetLaneData(HeaderType lane) => Lanes.GetLane((int)lane);

    #endregion

    #region Constructor

    public StarpowerInstrument(List<RawStarpowerEvent> starpowerEvents)
    {
        InstrumentID = HeaderType.Starpower;
        
        SetUpLanes();
        ParseRawStarpowerEvents(starpowerEvents);
    }

    public StarpowerInstrument()
    {
        InstrumentID = HeaderType.Starpower;
        
        SetUpLanes();
    }

    public StarpowerInstrument(List<PenguinEventSection> lanes)
    {
        InstrumentID = HeaderType.Starpower;
        
        Lanes = new Lanes<StarpowerEventData>(lanes, GenerateStarpowerLaneIDs());
        sustainer = new SustainHelper<StarpowerEventData>(this, Lanes, false);
    }

    private void SetUpLanes()
    {
        Lanes = new Lanes<StarpowerEventData>(GenerateStarpowerLaneIDs());
        sustainer = new SustainHelper<StarpowerEventData>(this, Lanes, false);
    }

    private static List<int> GenerateStarpowerLaneIDs()
    {
        List<int> headerTypeIDs = new();
        foreach (var instrumentType in Enum.GetValues(typeof(HeaderType)))
        {
            // instruments begin at 10^1. Refer to HeaderType for specifics.
            if ((int)instrumentType < 10) continue;
            headerTypeIDs.Add((int)instrumentType);
        }

        return headerTypeIDs;
    }

    #endregion
    
    #region Selections
    
    // Pull this up to IInstrument
    public void ClearLaneSelection(HeaderType lane) => Lanes.GetLaneSelection((int)lane).Clear();

    public void CopySelectionTo(InstrumentType targetInstrument, HashSet<DifficultyType> targetDifficulties)
    {
        var undoAction = new SelectionChangeSnapshot(this, LaneController);

        var selectionData = Lanes.GetUnifiedSelectionWithData();
        
        foreach (var trackDiff in targetDifficulties)
        {
            Lanes.CopySelectionToLane(
                (int)InstrumentMetadata.GetHeader(targetInstrument, trackDiff), 
                selectionData
                );
        }

        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);
    }

    public void MoveSelectionTo(InstrumentType targetInstrument, HashSet<DifficultyType> targetDifficulties)
    {
        var undoAction = new SelectionChangeSnapshot(this, LaneController);

        var selectionData = Lanes.CutUnifiedSelectionWithData();
        
        foreach (var trackDiff in targetDifficulties)
        {
            Lanes.CopySelectionToLane(
                (int)InstrumentMetadata.GetHeader(targetInstrument, trackDiff), 
                selectionData
                );
        }
        
        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);
    }

    #region Internal Overrides
    
    // Don't use this without making sure it's absolutely necessary.
    // The two functions above, <Action>SelectionTo, are more aligned to this use case.
    protected override void InternalSetSelectionToNewLane(int destinationLane) => Lanes.SetSelectionToNewLane(destinationLane);

    protected override List<int> targetLanes => InstrumentSpawningManager.instance.GetActiveInstrumentIDs();

    #endregion
    
    public void MakeSelectionUnison()
    {
        var undoAction = new SelectionChangeSnapshot(this, LaneController);
        
        var selections = Lanes.GetTotalSelectionByLane();
        var minMaxTicks = Lanes.GetSelectionBounds();

        foreach (var selection in selections)
        {
            var selectionVal = selection.Value;

            foreach (var tick in selectionVal)
            {
                if (!Lanes.TryGetTick(selection.Key, tick, out var starpowerEventData))
                {
                    continue;
                }

                Lanes.CopyDataToAllLanes(selection.Key, tick);
            }
        }
        
        ValidateSustainsInRange(minMaxTicks);
        
        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);
        
        Chart.InPlaceRefresh();
    }

    public void IsolateSelection()
    {
        var undoAction = new SelectionChangeSnapshot(this, LaneController);
        
        var selections = Lanes.GetTotalSelectionByLane();
        var minMaxTicks = Lanes.GetSelectionBounds();

        foreach (var selection in selections)
        {
            var selectionVal = selection.Value;
            
            foreach (var tick in selectionVal)
            {
                Lanes.DeleteAllEventsInTickDataRangeNotSelected(selection.Key, tick);
            }
        }
        
        ValidateSustainsInRange(minMaxTicks);
        
        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);
        
        Chart.InPlaceRefresh();
    }

    #endregion

    #region Add/Delete
    
    protected override void InternalAddDataChecks(int tick, int lane)
    {
        ClampSustainsBefore(tick, lane);
    }

    #endregion

    #region Moving

    protected override LinkedList<int> GetLaneProgression()
    {
        return 
            InstrumentSpawningManager.instance is null ? 
                new LinkedList<int>() : 
                InstrumentSpawningManager.instance.GetCurrentInstrumentOrdering();
    }

    #endregion

    #region Import

    // RawStarpowerEvent comes from ChartParser.
    // To parse starpower as a separate track, ChartParser checks every incoming event to see if it is starpower
    // and then packs it as RawStarpower, which is then unpacked here.
    // Since the data structure of PCE is very different to the structure of a .chart file, this half&half parsing method is what came to be.
    // AddChartFormatted comes from Clipboard, which parses the lines and then parses valid data.
    // Two pathes share some common actions which is why the flow is a bit weird with TryParses.

    private void ParseRawStarpowerEvents(List<RawStarpowerEvent> starpowerEvents)
    {
        foreach (var @event in starpowerEvents)
        {
            var data = @event.data.Split(" ");

            // S identifier should already be checked by ChartParser

            if (!TryParseCheckedLine(data, out var parsedData)) continue;

            Lanes.GetLane((int)@event.header).Add(@event.tick, parsedData);
        }
    }

    protected override void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines)
    {
        throw new NotImplementedException(
            "Clipboard logic should be overridden for SP instrument. " +
            "SP data is dealt with on an instrument:data basis, not through a list of raw events.");
    }

    private void AddChartFormattedEventsToInstrument(Dictionary<HeaderType, List<KeyValuePair<int, string>>> chartData, int offset)
    {
        foreach (var headerData in chartData)
        {
            if (headerData.Value.Count == 0) continue;
            HashSet<int> ticks = headerData.Value.Select(item => item.Key).ToHashSet();

            var targetLane = GetLaneData(headerData.Key);
            targetLane.PopTicksInRange(ticks.Min(), ticks.Max());

            foreach (var @event in headerData.Value)
            {
                if (!TryParseEventLineValue(@event.Value, out var data))
                {
                    continue;
                }
                targetLane.Add(@event.Key + offset, data);
            }
        }
        // fixme: calculate range properly
        ValidateSustainsInRange(0, SongTime.SongLengthTicks);
        Chart.InPlaceRefresh();
    }

    private StarpowerEventData defaultSPEvent = new(false, -1);
    private static readonly string[] validStarpowerEvents = new string[2] { STARPOWER_ID, DRUM_FILL_ID };

    public static bool IsSpecialEventStarpowerEvent(string[] partiallyParsedVals)
    {
        return validStarpowerEvents.Contains(partiallyParsedVals[1]);
    }

    private bool TryParseEventLineValue(string line, out StarpowerEventData data)
    {
        data = defaultSPEvent;

        if (!line.Contains('S'))
        {
            return false;
        }

        var vals = line.Split(' ');

        if (vals[ChartParser.INDENTIFIER_INDEX] != "S") return false;
        if (!IsSpecialEventStarpowerEvent(vals)) return false;

        if (TryParseCheckedLine(vals, out data))
        {
            return true;
        }
        return false;
    }

    public bool TryParseCheckedLine(string[] splitVal, out StarpowerEventData data)
    {
        data = defaultSPEvent;

        var fill = splitVal[EVENT_TYPE_IDENTIFIER_INDEX] == DRUM_FILL_ID;

        if (!int.TryParse(splitVal[SUSTAIN_INDEX], out int sustain))
        {
            Debug.LogError($"Invalid sustain. Expected integer, given {splitVal[2]}.");
            return false;
        }

        data = new StarpowerEventData(fill, sustain);
        return true;
    }

    protected override PasteSnapshot AddChartFormattedEventsToInstrument(string clipboardData, int offset)
    {
        var clipboardAsLines = clipboardData.Split(Environment.NewLine);

        List<KeyValuePair<int, string>> activeSection = null;
        HeaderType sectionID = (HeaderType)(-1);

        Dictionary<HeaderType, List<KeyValuePair<int, string>>> parsedSections = new();

        for (int i = 0; i < clipboardAsLines.Length; i++)
        {
            var workingLine = clipboardAsLines[i];
            if (activeSection == null && workingLine.Contains("["))
            {
                if (InstrumentMetadata.TryParseHeaderType(workingLine, out sectionID))
                {
                    activeSection = new List<KeyValuePair<int, string>>();
                    i++; // avoid '{'
                }
                continue;
            }
            if (activeSection != null)
            {
                if (workingLine.Contains("}"))
                {
                    parsedSections.Add(sectionID, activeSection);
                    activeSection = null;
                    sectionID = (HeaderType)(-1);
                }
                else
                {
                    if (InstrumentMetadata.TryParseChartLine(workingLine, out var formattedKVP))
                    {
                        activeSection.Add(formattedKVP);
                    }
                }
            }
        }

        var min = parsedSections.Values.SelectMany(list => list).Min(kvp => (int?)kvp.Key);
        var max = parsedSections.Values.SelectMany(list => list).Max(kvp => (int?)kvp.Key);

        if (min is null || max is null) return null;
        var prePaste = LaneController.PopTicksInRange((int)min, (int)max);
        
        AddChartFormattedEventsToInstrument(parsedSections, offset);
        
        var postPaste = LaneController.PeekTicksInRange((int)min, (int)max);
        return new PasteSnapshot(this, new PasteDataPackage(prePaste, postPaste));
    }

    #endregion

    #region Export 

    public override string ConvertSelectionToString()
    {
        StringBuilder stringifiedOutput = new();

        foreach (var selectionKVP in Lanes.ExportNormalizedSelection())
        {
            if (selectionKVP.Value.Count == 0) continue;

            InstrumentMetadata.CreateHeader(stringifiedOutput, (HeaderType)selectionKVP.Key);

            var selectionData = selectionKVP.Value;

            foreach (var @event in selectionData)
            {
                stringifiedOutput.AppendLine(InstrumentMetadata.MakeChartLine(@event.Key, @event.Value.ToChartFormat(int.MinValue)[0]));
            }

            InstrumentMetadata.CloseSection(stringifiedOutput);
        }

        return stringifiedOutput.ToString();
    }

    public List<string> ExportInstrumentStarpowerData(HeaderType instrumentID)
    {
        var outputList = new List<string>();

        if ((int)instrumentID <= 10) return outputList;

        foreach (var @event in Lanes.GetLane((int)instrumentID))
        {
            outputList.Add($"\t{@event.Key} = {@event.Value.ToChartFormat(0)[0]}");
        }

        return outputList;
    }

    public override IInstrument DuplicateToNewInstrument(HeaderType newInstrumentID)
    {
        throw new ArgumentException("StarpowerInstruments cannot be duplicated in the same chart file.");
    }

    #endregion

    #region Not Implemented

    public override ILaneData GetBarLaneData() =>
        throw new NotImplementedException($"Starpower does not have a bar lane. Please format the note receivers to access your intended instrument instead of the loaded instrument.");

    public override SoloDataSet SoloData
    {
        get => null;
        set {}
    }    

    #endregion

    public override int MatchXCoordinateToLane(float xCoordinate)
    {
        Debug.Assert(InstrumentSpawningManager.instance is not null);

        if (InstrumentSpawningManager.instance.IsAnyTrackLoaded())
        {
            return InstrumentSpawningManager.instance.leftmostTrack.MatchXCoordinateToLane(xCoordinate);
        }

        return 1;
    }
}
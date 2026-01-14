using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Notes: The equivilent of LaneOrientation in this instrument is HeaderType - as each instrument track has independent starpower.
/// </summary>
public class StarpowerInstrument : IInstrument, ISustainableInstrument
{
    #region Constants

    private const int EVENT_TYPE_IDENTIFIER_INDEX = 1;
    private const int SUSTAIN_INDEX = 2;
    private const string DRUM_FILL_ID = "64";
    private const string STARPOWER_ID = "2";

    #endregion

    #region Data Access

    /// <summary>
    /// Access instrument data with GetLane(int), where int is casted version of HeaderType, since each traditional instrument has its own set of starpower events.
    /// </summary>
    private Lanes<StarpowerEventData> Lanes;
    ILaneData IInstrument.GetLaneData(int lane) => Lanes.GetLane(lane);
    ILaneData IInstrument.GetBarLaneData()
    {
        throw new NotImplementedException($"Starpower does not have a bar lane. Please format the note receivers to access your intended instrument instead of the loaded instrument.");
    }
    ISelection IInstrument.GetLaneSelection(int lane) => Lanes.GetLaneSelection(lane);

    public LaneSet<StarpowerEventData> GetLaneData(HeaderType lane) => Lanes.GetLane((int)lane);
    public LaneSet<StarpowerEventData> GetLaneData(int lane) => Lanes.GetLane(lane);

    public SelectionSet<StarpowerEventData> GetLaneSelection(HeaderType lane) => Lanes.GetLaneSelection((int)lane);
    public SelectionSet<StarpowerEventData> GetLaneSelection(int lane) => Lanes.GetLaneSelection(lane);

    public SoloDataSet SoloData
    {
        get { throw new NotImplementedException("Starpower does not have solo events. If you are using the SoloEvent suite, it is not required."); }
        set { throw new NotImplementedException("Starpower does not have solo events. If you are using the SoloEvent suite, it is not required."); }
    }
    public InstrumentType InstrumentName { get; set; } = InstrumentType.starpower;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;
    public HeaderType InstrumentID => HeaderType.Starpower;

    public int NoteSelectionCount => Lanes.GetTotalSelectionCount();

    public List<int> GetUniqueTickSet() => Lanes.GetUniqueTickSet();

    #endregion

    #region Constructor

    public StarpowerInstrument(List<RawStarpowerEvent> starpowerEvents)
    {
        SetUpLanes();
        ParseRawStarpowerEvents(starpowerEvents);
    }

    void SetUpLanes()
    {
        List<int> headerTypeIDs = new();
        foreach (var instrumentType in Enum.GetValues(typeof(HeaderType)))
        {
            // instruments begin at 10^1. Refer to HeaderType for specifics.
            if ((int)instrumentType < 10) continue;
            headerTypeIDs.Add((int)instrumentType);
        }
        Lanes = new(headerTypeIDs);

        sustainer = new(this, Lanes, false);
    }


    InputMap inputMap;
    public void SetUpInputMap()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.XYDrag.performed += x => MoveSelection();
        inputMap.Charting.LMB.canceled += x => CompleteMove();
        inputMap.Charting.Delete.performed += x => DeleteSelection();
        inputMap.Charting.SustainDrag.performed += x => sustainer.SustainSelection();
        inputMap.Charting.LMB.performed += x => CheckForSelectionClear();
        inputMap.Charting.SelectAll.performed += x => Lanes.SelectAll();
    }

    #endregion

    #region Sustains

    SustainHelper<StarpowerEventData> sustainer;

    public void ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event) => sustainer.ChangeSustainFromTrail(pointerEventData, @event);
    public int CalculateSustainClamp(int sustainLength, int tick, int lane) => sustainer.CalculateSustainClamp(sustainLength, tick, lane);
    public int CalculateSustainClamp(int sustainLength, int tick, HeaderType lane) => CalculateSustainClamp(sustainLength, tick, (int)lane);
    void ValidateSustainsInRange(int startTick, int endTick) => sustainer.ValidateSustainsInRange(startTick, endTick);
    #endregion

    #region Selections

    public void ClearAllSelections() => Lanes.ClearAllSelections();

    void CheckForSelectionClear()
    {
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || Chart.instance.SceneDetails.IsMasterHighwayHit()) return;

        ClearAllSelections();
        Chart.InPlaceRefresh();
    }

    void DeleteSelection()
    {
        if (Chart.LoadedInstrument != this) return;

        Lanes.DeleteAllTicksInSelection();

        Chart.InPlaceRefresh();
    }

    public bool NoteSelectionContains(int tick, int lane) => Lanes.GetLaneSelection(lane).Contains(tick);

    public void ClearTickFromAllSelections(int tick) => Lanes.ClearTickFromAllSelections(tick);

    public void ShiftClickSelectLane(int start, int end, int lane)
    {
        Lanes.GetLaneSelection(lane).ShiftClickSelectInRange(start, end);
    }

    public void ShiftClickSelect(int start, int end)
    {
        Lanes.ShiftClickSelect(start, end, InstrumentSpawningManager.instance.GetActiveInstrumentIDs());
    }

    public void ShiftClickSelect(int tick)
    {
        Lanes.ShiftClickSelect(tick, tick, InstrumentSpawningManager.instance.GetActiveInstrumentIDs());
    }


    #endregion

    #region Add/Delete
    public void DeleteAllEventsAtTick(int tick)
    {
        Lanes.PopAllEventsAtTick(tick);
        Chart.InPlaceRefresh();
    }

    public void DeleteTickInLane(int tick, int lane)
    {
        Lanes.PopTickFromLane(tick, lane);
        Chart.InPlaceRefresh();
    }

    public void DeleteTicksInSelection() => Lanes.DeleteAllTicksInSelection();

    #endregion

    #region Moving

    MoveHelper<StarpowerEventData> mover = new();
    LinkedList<int> currentLaneOrdering = null;

    void MoveSelection()
    {
        if (Chart.LoadedInstrument != this) return;
        currentLaneOrdering ??= InstrumentSpawningManager.instance.GetCurrentInstrumentOrdering();
        if (mover.Move2DSelection(this, Lanes, currentLaneOrdering))
        {
            Chart.InPlaceRefresh();
        }
    }

    void CompleteMove()
    {
        if (this != Chart.LoadedInstrument) return;
        Chart.showPreviewers = true;

        if (!mover.MoveInProgress) return;
        currentLaneOrdering = null;

        // ValidateSustainsInRange(mover.GetFinalValidationRange(laneOrdering));
        mover.Reset();
    }

    #endregion

    #region Import

    // RawStarpowerEvent comes from ChartParser.
    // To parse starpower as a separate track, ChartParser checks every incoming event to see if it is starpower
    // and then packs it as RawStarpower, which is then unpacked here.
    // Since the data structure of PCE is very different to the structure of a .chart file, this half&half parsing method is what came to be.
    // AddChartFormatted comes from Clipboard, which parses the lines and then parses valid data.
    // Two pathes share some common actions which is why the flow is a bit weird with TryParses.

    void ParseRawStarpowerEvents(List<RawStarpowerEvent> starpowerEvents)
    {
        foreach (var @event in starpowerEvents)
        {
            var data = @event.data.Split(" ");

            // S identifier should already be checked by ChartParser

            if (!TryParseCheckedLine(data, out var parsedData)) continue;

            Lanes.GetLane((int)@event.header).Add(@event.tick, parsedData);
        }
    }

    public void AddChartFormattedEventsToInstrument(Dictionary<HeaderType, List<KeyValuePair<int, string>>> chartData, int offset)
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
    }

    StarpowerEventData defaultSPEvent = new(false, -1);
    public static readonly string[] validStarpowerEvents = new string[2] { STARPOWER_ID, DRUM_FILL_ID };

    public static bool IsSpecialEventStarpowerEvent(string[] partiallyParsedVals)
    {
        return validStarpowerEvents.Contains(partiallyParsedVals[1]);
    }

    bool TryParseEventLineValue(string line, out StarpowerEventData data)
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

        data = new(fill, sustain);
        return true;
    }

    public void AddChartFormattedEventsToInstrument(string clipboardData, int offset)
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
                    activeSection = new();
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

        AddChartFormattedEventsToInstrument(parsedSections, offset);
    }

    #endregion

    #region Export 

    public string ConvertSelectionToString()
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

    public List<string> ExportAllEvents()
    {
        throw new System.NotImplementedException();
    }

    #endregion
}
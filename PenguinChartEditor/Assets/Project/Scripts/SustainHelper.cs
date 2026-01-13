using System.Collections.Generic;
using System.Linq;

public class SustainHelper<T> where T : IEventData, ISustainable
{
    SustainData<T> sustainData = new();

    Lanes<T> laneData;
    IInstrument parentInstrument;

    bool obeyExtendedSustainSetting;

    public SustainHelper(IInstrument parentInstrument, Lanes<T> lanes, bool obeyExtended)
    {
        obeyExtendedSustainSetting = obeyExtended;
        laneData = lanes;
        this.parentInstrument = parentInstrument;
    }

    public void ResetSustainChange()
    {
        sustainData = new();
    }

    public static int GetCurrentMouseTick()
    {
        var newHighwayPercent = Chart.instance.SceneDetails.GetCursorHighwayProportion();

        // 0 is very unlikely as an actual position (as 0 is at the very bottom of the TRACK, which should be outside the screen in most cases)
        // but is returned if cursor is outside track
        // min value serves as an easy exit check in case the cursor is outside the highway
        if (newHighwayPercent == 0) return int.MinValue;

        return SongTime.CalculateGridSnappedTick(newHighwayPercent);
    }

    public void SustainSelection()
    {
        if (Chart.LoadedInstrument != parentInstrument || !Chart.IsModificationAllowed()) return;

        // Early return if attempting to start an edit while over an overlay element
        // Allows edit to start only if interacting with main content
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() && !sustainData.sustainInProgress) return;

        var currentMouseTick = GetCurrentMouseTick();
        if (currentMouseTick == int.MinValue) return;

        // early return if no changes to mouse's grid snap
        if (currentMouseTick == sustainData.lastMouseTick) return;

        if (!sustainData.sustainInProgress)
        {
            // directly access parent lane here to avoid reassigning the local shortcut variable
            sustainData = new(laneData.GetTotalSelectionByLane(), currentMouseTick);
            return;
        }

        foreach (var lane in laneData.ExportData())
        {
            var laneTicks = sustainData.sustainingTicks[lane.Key];
            var workingLane = lane.Value;

            foreach (var tick in laneTicks)
            {
                var newSustain = currentMouseTick - tick;

                // drag behind the note to max out sustain - cool feature from moonscraper
                // -CurrentDivison is easy arbitrary value for when to max out - so that there is a buffer for users to remove sustain entirely
                // SongLengthTicks will get clamped to max sustain length
                if (newSustain < -DivisionChanger.CurrentDivision) newSustain = SongTime.SongLengthTicks;

                if (workingLane.ContainsKey(tick))
                {
                    UpdateSustain(tick, lane.Key, newSustain);
                }
            }
        }

        Chart.InPlaceRefresh();
        sustainData.lastMouseTick = currentMouseTick;
    }

    public void UpdateSustain(int tick, int lane, int newSustain)
    {
        // clamp based on this lane only (ignore other lane overlap)
        if (UserSettings.ExtSustains || !obeyExtendedSustainSetting)
        {
            var currentLane = laneData.GetLane(lane);
            if (!currentLane.Contains(tick)) return;

            currentLane[tick] = (T)currentLane[tick].ExportWithNewSustain(
                CalculateSustainClamp(newSustain, tick, lane)
                );
        }
        // clamp based on ALL lanes
        else
        {
            var ticks = laneData.GetTickEventBounds(tick);
            var calculatedCurrentSustain = CalculateSustainClamp(newSustain, tick, lane);

            foreach (var lanePairing in laneData.LaneKeys)
            {
                var iteratorLane = laneData.GetLane(lanePairing);

                if (iteratorLane.Contains(tick))
                {
                    iteratorLane[tick] = (T)iteratorLane[tick].ExportWithNewSustain(calculatedCurrentSustain);
                }
            }
        }
    }

    public int CalculateSustainClamp(int sustainLength, int tick, int lane)
    {
        int nextTick =
            UserSettings.ExtSustains || !obeyExtendedSustainSetting ? 
            laneData.GetLane(lane).GetNextTickEventInLane(tick) : 
            laneData.GetTickEventBounds(tick).next;

        int clampedSustain = sustainLength;
        if (nextTick != LaneSet<FiveFretNoteData>.NO_TICK_EVENT)
        {
            if (sustainLength + tick >= nextTick - UserSettings.SustainGapTicks)
            {
                clampedSustain = (nextTick - tick) - UserSettings.SustainGapTicks;
            }
        }
        else
        {
            if (sustainLength + tick >= SongTime.SongLengthTicks)
            {
                clampedSustain = (SongTime.SongLengthTicks - tick); // does sustain gap apply to end of song? 🤔
            }
        }

        if (clampedSustain < 0) clampedSustain = 0;

        var sustainLengthMS = Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick + clampedSustain) - Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick);
        return sustainLengthMS < UserSettings.MINIMUM_SUSTAIN_LENGTH_SECONDS ? 0 : clampedSustain;
    }

    public void ShiftClickSustainClamp(int tick, int tickLength)
    {
        foreach (var lane in laneData.LaneKeys)
        {
            if (laneData.GetLane(lane).ContainsKey(tick))
            {
                laneData.GetLane(lane)[tick] = (T)laneData.GetLane(lane)[tick].ExportWithNewSustain(tickLength);
            }
        }
    }

    public void ValidateSustain(int tick, int lane)
    {
        if (UserSettings.ExtSustains || !obeyExtendedSustainSetting)
        {
            var extendedLaneRef = laneData.GetLane(lane);
            if (!extendedLaneRef.Contains(tick)) return;

            UpdateSustain(tick, lane, extendedLaneRef[tick].Sustain);
        }
        else
        {
            var smallestSustain = int.MaxValue;

            foreach (var laneKey in laneData.LaneKeys)
            {
                var laneRef = laneData.GetLane(laneKey);
                if (!laneRef.Contains(tick)) continue;

                if (laneRef[tick].Sustain < smallestSustain) smallestSustain = laneRef[tick].Sustain;
            }

            UpdateSustain(tick, lane, smallestSustain);
        }
    }

    public void ValidateSustainsInRange(MinMaxTicks range) => ValidateSustainsInRange(range.min, range.max);
    public void ValidateSustainsInRange(int startTick, int endTick)
    {
        var uniqueTicks = laneData.GetUniqueTickSet();
        var uniqueTicksInRange = uniqueTicks.Where(tick => tick >= startTick && tick <= endTick).ToList();

        if (UserSettings.ExtSustains || !obeyExtendedSustainSetting)
        {
            foreach (var laneKey in laneData.LaneKeys)
            {
                var currentLane = laneData.GetLane(laneKey);
                ClampSustainsBefore(startTick, laneKey);

                for (int index = 0; index < uniqueTicksInRange.Count(); index++)
                {
                    int tick = uniqueTicksInRange[index];
                    if (currentLane.Contains(tick)) ValidateSustain(uniqueTicksInRange[index], laneKey);
                }
            }
        }
        else
        {
            // lane orientation is irrelevant, just pass in anything
            ClampSustainsBefore(startTick, 0);

            for (int index = 0; index < uniqueTicksInRange.Count(); index++)
            {
                int tick = uniqueTicksInRange[index];
                ValidateSustain(uniqueTicksInRange[index], 0);
            }
        }

        Chart.InPlaceRefresh();
    }

    public void ClampSustainsBefore(int tick, int lane)
    {
        if (UserSettings.ExtSustains)
        {
            ClampLaneEventsBefore(tick, lane);
            return;
        }

        foreach (var laneKey in laneData.LaneKeys)
        {
            ClampLaneEventsBefore(tick, laneKey);
        }
    }

    void ClampLaneEventsBefore(int tick, int lane)
    {
        var currentLane = laneData.GetLane(lane);

        var clampTargetTick = currentLane.GetPreviousTickEventInLane(tick);
        if (clampTargetTick == LaneSet<FiveFretNoteData>.NO_TICK_EVENT) return;

        var data = currentLane[clampTargetTick];
        currentLane[clampTargetTick] = (T)data.ExportWithNewSustain(
            CalculateSustainClamp(data.Sustain, clampTargetTick, lane)
            );
    }

    public void SetSelectionSustain(int ticks)
    {
        var currentSelection = laneData.GetTotalSelectionByLane();

        foreach (var laneKey in laneData.LaneKeys)
        {
            var laneSelection = currentSelection[laneKey];
            if (laneSelection.Count == 0) continue;

            var changingLane = laneData.GetLane(laneKey);

            foreach (var selectedNote in laneSelection)
            {
                UpdateSustain(selectedNote, laneKey, ticks);
            }
        }

        Chart.InPlaceRefresh();
    }

}
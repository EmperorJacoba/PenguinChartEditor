using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(FiveFretNote))]
public class FiveFretNotePreviewer : Previewer
{
    [SerializeField] FiveFretNote note;
    [SerializeField] Transform highway;
    [SerializeField] PhysicsRaycaster cameraHighwayRaycaster;
    [SerializeField] FiveFretLane lane;
    [SerializeField] int laneCenterPosition;

    public override void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!IsPreviewerActive(percentOfScreenVertical, percentOfScreenHorizontal)) return;

        var hitPosition = GetHighwayPosition();
        var highwayProportion = hitPosition.z / highway.localScale.z;
        if (highwayProportion == 0) return;

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);

        note.Tick = Tick;
        if (note.GetEventSet().ContainsKey(Tick))
        {
            note.Visible = false;
            return;
        }

        note.UpdatePosition(
            (Tempo.ConvertTickTimeToSeconds(Tick) - Waveform.startTime) / Waveform.timeShown,
            highway.localScale.z,
            laneCenterPosition);

        var lowerBound = laneCenterPosition - 1;
        var upperBound = laneCenterPosition + 1;
        

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (hitPosition.x < lowerBound || hitPosition.x > upperBound) // needs update
        {
            note.Visible = false;
        }
        else // optimize this
        {
            note.Visible = true;
        }
    }

    Vector3 GetHighwayPosition()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(pointerData, results);

        if (results.Count == 0) return Vector3.zero;

        return results[0].worldPosition;
    }

    public override void CreateEvent()
    {
        if (IsOverlayUIHit()) return;

        if (note.Visible && !note.GetEventSet().ContainsKey(note.Tick))
        {
            note.CreateEvent(note.Tick, new FiveFretNoteData(0, FiveFretNoteData.FlagType.strum)); // strum flag needs to be changed
            Chart.Refresh();
            disableNextSelectionCheck = true;
        }
    }
    public override void Hide()
    {
        if (note.Visible) note.Visible = false;
    }
    public override void Show()
    {
        if (!note.Visible) note.Visible = true;
    }

    protected override void Awake()
    {
        base.Awake();
        var fiveFretNote = GetComponent<FiveFretNote>();
        fiveFretNote.lanePreviewer = this;
        fiveFretNote.laneIdentifier = lane.laneIdentifier;
    }
}

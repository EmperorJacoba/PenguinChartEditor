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

        var hitPosition = GetCursorHighwayPosition();
        var highwayProportion = GetCursorHighwayProportion();
        if (highwayProportion == 0)
        {
            Hide(); return;
        }

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);

        if (note.LaneData.ContainsKey(Tick))
        {
            Hide(); return;
        }
        note.UpdatePosition(Waveform.GetWaveformRatio(Tick), highway.localScale.z, note.XCoordinate);

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

    private Vector3 GetCursorHighwayPosition()
    {
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(pointerData, results);

        if (results.Count == 0) return new Vector3(int.MinValue, int.MinValue, int.MinValue);

        return results[0].worldPosition;
    }

    /// <summary>
    /// Get the highway proportion but set the X value of the raycast to the center of the screen.
    /// </summary>
    /// <returns></returns>
    public override float GetCursorHighwayProportion()
    {
        PointerEventData modifiedPointerData = new(EventSystem.current)
        {
            position = new(Screen.width / 2, Input.mousePosition.y)
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(modifiedPointerData, results);

        if (results.Count == 0) return 0;
        return results[0].worldPosition.z / highway.localScale.z;
    }

    public override void CreateEvent()
    {
        if (IsOverlayUIHit()) return;

        if (note.Visible && !note.LaneData.ContainsKey(note.Tick))
        {
            note.CreateEvent(Tick, new FiveFretNoteData(0, FiveFretNoteData.FlagType.strum)); // strum flag needs to be changed

            // please actually find the root cause of this issue
            // this is a fix for the event randomly being selected when the event is created (? - I mean, obviously not ACTUALLY random, but it doesn't always happen?)
            // I put checks everywhere I can think of to stop this and it will not obey me in the way in which I intend
            // so nip it in the bud by putting a check right at the source (even though this functionality is better suited elsewhere)
            note.Selection.Remove(Tick);

            disableNextSelectionCheck = true;
            Chart.Refresh();
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

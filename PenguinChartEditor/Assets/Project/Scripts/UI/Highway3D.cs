using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IHighway
{
    public float Length { get; }
}
public class Highway3D : MonoBehaviour, IPointerDownHandler
{
    public InstrumentCategory highwayDisplayType;
    public int laneWidth;
    public float Length
    {
        get
        {
            return transform.localScale.z;
        }
        private set
        {
            if (transform.localScale.z == value) return;
            transform.localScale = new(transform.localScale.x, transform.localScale.y, value);
        }
    }
    
    public delegate void LengthUpdateDelegate();
    public static event LengthUpdateDelegate HighwayLengthChanged;
    public static float highwayLength
    {
        get
        {
            return _hL;
        }
        set 
        {
            if (_hL == value) return;
            _hL = value;
            HighwayLengthChanged?.Invoke();
        }
    }

    private static float _hL = 75;

    public void Awake()
    {
        HighwayLengthChanged += UpdateLength;
    }

    void UpdateLength()
    {
        Length = _hL;
        Waveform.GenerateWaveformPoints();
        Chart.InPlaceRefresh();
    }

    public float LeftEndCoordinate => -(transform.localScale.x / 2);
    public float RightEndCoordinate => transform.localScale.x / 2;

    public float GetCenterXCoordinateFromLane(int lane)
    {
        if (laneWidth <= 0) throw new System.NullReferenceException("Lane width cannot be less than or equal to zero. Please set the lane width of this scene in this scene's SceneDetails game object.");

        if (highwayDisplayType == InstrumentCategory.FiveFret)
        {
            // This is to make up for the fact that LaneOrientation sets open to position 0
            // for pitch reasons. Green is effectively lane zero but pitch says otherwise
            var laneOneCenterCoordinate = LeftEndCoordinate + (laneWidth / 2);

            if (UserSettings.OpenNoteAsFret)
            {
                if (lane == (int)FiveFretInstrument.LaneOrientation.open) return laneOneCenterCoordinate;
            }
            else // center is 0 for the open bar note
            {
                if (lane == (int)FiveFretInstrument.LaneOrientation.open) return 0;
                lane--;
            }

            return laneOneCenterCoordinate + (laneWidth * lane);
        }

        throw new System.ArgumentException(
            "You trying to get the center coordinate of a lane with a scene that has either a) not been set up properly or b) " +
            "does not use traditional lanes (like TempoMap). Refer to GetCenterXCoordinateFromLane() for more info."
            );
    }

    public float GetStarpowerXCoordinate()
    {
        return LeftEndCoordinate - 1.5f;
    }

    public int MatchXCoordinateToLane(float xCoordinate)
    {
        if (highwayDisplayType == InstrumentCategory.FiveFret)
        {
            // Isolated algebraically & through testing. Works for any x coordinate on the highway (secret or visible).
            return (int)Mathf.Floor((xCoordinate - LeftEndCoordinate) / laneWidth);
        }
        throw new System.Exception("Error when matching X coordinate to a lane. Cursor is within highway bounds but not within a lane.");
    }

    // Note: for these two functions (this and the one below)
    // If highways are not centered at z = 0, then the worldPosition.z / Length will not be accurate
    public Vector3 GetCursorHighwayPosition()
    {
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0) return new Vector3(int.MinValue, int.MinValue, int.MinValue);

        var relevantResult = results.Find(x => x.gameObject.transform.IsChildOf(transform));
        return relevantResult.worldPosition;
    }

    public float GetCursorHighwayProportion()
    {
        PointerEventData modifiedPointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(modifiedPointerData, results);

        if (results.Count == 0) return 0;

        var relevantResult = results.Find(x => x.gameObject.transform.IsChildOf(transform));

        return relevantResult.worldPosition.z / Length;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        Chart.LoadedInstrument.ClearAllSelections();
    }
}

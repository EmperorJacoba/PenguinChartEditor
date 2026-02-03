using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoloPlate : Event<SoloEventData>
{
    protected override bool HasSustainTrail => false;
    [SerializeField] private TMP_Text percentage;
    [SerializeField] private TMP_Text counter;

    protected override void InitializeEvent()
    {
        var ticks = ParentInstrument.GetUniqueTickSet();
        var totalNotes = ticks.Count(x => x >= representedData.StartTick && x <= representedData.EndTick);
        var notesHit = ticks.Count(x => x >= representedData.StartTick && x <= SongTime.SongPositionTicks);

        percentage.text = $"{Mathf.Floor((notesHit / (float)totalNotes) * 100)}%";
        counter.text = $"{notesHit} / {totalNotes}";
    }

    protected override void InitializeEventAsPreviewer()
    {
        
    }

    protected override void UpdatePosition()
    {
        float zPosition;
        if (SongTime.SongPositionTicks < Tick)
        {
            zPosition = GetDefaultZ();
        }
        else if (SongTime.SongPositionTicks > representedData.EndTick)
        {
            zPosition = (float)(Waveform.GetWaveformRatio(representedData.EndTick) * Highway3D.highwayLength);
        }
        else
        {
            zPosition = Mathf.Floor((float)Waveform.GetWaveformRatio(SongTime.SongPositionTicks) * Highway3D.highwayLength);
        }

        transform.position = 
            new Vector3(
                transform.position.x, 
                transform.position.y, 
                zPosition
                );
    }

    public override int Lane
    {
        get => IInstrument.SOLO_DATA_LANE_ID;
        set {} // not needed
    }

    public GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;
    public override IInstrument ParentInstrument => parentGameInstrument.representedInstrument;


    public override SelectionSet<SoloEventData> Selection => ParentInstrument.SoloData.SelectedStartEvents;

    protected override LaneSet<SoloEventData> LaneData => ParentInstrument.SoloData.SoloEvents;

    public SoloPreviewer previewer
    {
        get => _prevobj;
        set
        {
            if (_prevobj == value) return;
            _prevobj = value;
        }
    } // define in pooler

    private SoloPreviewer _prevobj;


    public override void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(1) && eventData.button == PointerEventData.InputButton.Left)
        {
            var targetEvent = LaneData.Where(x => x.Value.StartTick == Tick).ToList();
            if (targetEvent.Count == 0) return;

            LaneData.Remove(targetEvent[0]);
            return;
        }

        CalculateSelectionStatus(eventData);
    }

    public override void CreateEvent(int newTick, SoloEventData newData) { } // please remove
}

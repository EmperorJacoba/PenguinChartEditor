using System;
using UnityEngine;

public class HighwayPointer : MonoBehaviour
{
    [SerializeField] private bool appear2D;
    [SerializeField] private GameObject leftPointer;
    [SerializeField] private GameObject rightPointer;

    private GameInstrument parentInstrument;

    private float HighwayOffset => appear2D ? 0.25f : 0;

    private void Start()
    {
        parentInstrument = GetComponentInParent<GameInstrument>();

        leftPointer.transform.localPosition = 
            new Vector3(
                parentInstrument.HighwayLeftEndCoordinate - HighwayOffset, 
                leftPointer.transform.localPosition.y, 
                leftPointer.transform.localPosition.z
                );

        rightPointer.transform.localPosition =
            new Vector3(
                parentInstrument.HighwayRightEndCoordinate + HighwayOffset,
                rightPointer.transform.localPosition.y, 
                rightPointer.transform.localPosition.z
                );
    }

    // Having this in the update function is unavoidable since this is expected to update to the mouse grid so much.
    // Also doesn't have much overhead weirdly enough. The main bottleneck would probably be the input field check if
    // there are too many since that's O(n). 
    // Side note on the song grid: as of writing this (2/16/26), Moonscraper 2 has an option to allow gridless
    // placing, based on the one trailer from 12/25/25. Why is that a feature? You're just encouraging poor charting practices...
    private void Update()
    {
        if (
            AudioManager.AudioPlaying || 
            (Chart.LoadedInstrument == Chart.SyncTrackInstrument && Chart.IsPlacementAllowed()) ||
            Chart.IsSceneOverlayUIHit() || 
            PenguinInputField.IsInputFieldActive()
            )
        {
            transform.position = invisiblePosition;
            return;
        }

        var prop = parentInstrument.GetCursorHighwayProportion();
        if (prop <= 0) return;
        
        var tick = SongTime.CalculateGridSnappedTick(prop);
        transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            (float)(parentInstrument.HighwayLength * Waveform.GetWaveformRatio(
                tick)
            )); 

        Previewer.previewTick = tick;
    }

    // Update() won't run if disabled. This is good enough. Causes a chuckle when you look at it in scene view.
    // What are you pointing at, buddy? The pit of eternal nothingness? Lol, lmao even. 
    private Vector3 invisiblePosition => new(transform.position.x, transform.position.y, -10);
}

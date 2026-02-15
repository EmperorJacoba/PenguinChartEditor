using System;
using UnityEngine;

public class HighwayPointer : MonoBehaviour
{
    [SerializeField] private GameObject leftPointer;
    [SerializeField] private GameObject rightPointer;

    private GameInstrument parentInstrument;

    private void Start()
    {
        parentInstrument = GetComponentInParent<GameInstrument>();

        leftPointer.transform.localPosition = 
            new Vector3(
                parentInstrument.HighwayLeftEndCoordinate, 
                leftPointer.transform.localPosition.y, 
                leftPointer.transform.localPosition.z
                );

        rightPointer.transform.localPosition =
            new Vector3(
                parentInstrument.HighwayRightEndCoordinate,
                rightPointer.transform.localPosition.y, 
                rightPointer.transform.localPosition.z
                );
    }

    private void Update()
    {
        if (AudioManager.AudioPlaying || Chart.instance.SceneDetails.IsSceneOverlayUIHit() || PenguinInputField.IsInputFieldActive())
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

    private Vector3 invisiblePosition => new(transform.position.x, transform.position.y, -10);
}

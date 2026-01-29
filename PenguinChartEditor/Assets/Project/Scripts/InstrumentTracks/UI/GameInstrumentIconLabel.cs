using System;
using UnityEngine;
using TMPro;

/// <summary>
/// The label displaying the instrument & difficulty corresponding to a track in multi-track (starpower) mode.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class GameInstrumentIconLabel : MonoBehaviour
{
    [SerializeField] public GameInstrument parentGameInstrument;
    [SerializeField] private InstrumentIcons iconMatcher;
    private MeshRenderer iconMesh;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private TMP_Text counter;

    private void Awake()
    {
        iconMesh = GetComponent<MeshRenderer>();

        iconMesh.material = iconMatcher.GetInstrumentIcon(parentGameInstrument.instrumentID);
        difficultyText.text = InstrumentMetadata.GetDifficultyAbbreviation(parentGameInstrument.instrumentID);

        eventSetRef = Chart.StarpowerInstrument.GetLaneData(parentGameInstrument.instrumentID);
    }

    private LaneSet<StarpowerEventData> eventSetRef;
    private int displayedEventCount = -1;
    private void Update()
    {
        if (displayedEventCount == eventSetRef.Count) return;
        
        // This has negligible effect running in Update. < 1 * 10^-2 milliseconds per frame. It's easier than setting up an event.
        counter.text = $"{eventSetRef.Count}";
        displayedEventCount = eventSetRef.Count;
    }
}
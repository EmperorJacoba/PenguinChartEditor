using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SongScrubber : MonoBehaviour
{
    [SerializeField] private Scrollbar scrubber;
    [SerializeField] private GameObject sideSectionPrefab;
    [SerializeField] private RectTransform slidingArea;

    private RectTransform rectTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        scrubber.onValueChanged.AddListener(x => UpdateSongTimeFromScrubber(x));
        
        SongTime.TimeChanged += UpdateSongScrubber;
        AudioManager.PlaybackStateChanged += SetScrubberInteractableState;
    }

    private void SetScrubberInteractableState(bool playbackState)
    {
        if (playbackState) scrubber.interactable = false;
        else scrubber.interactable = true;
    }

    private void OnDestroy()
    {
        SongTime.TimeChanged -= UpdateSongScrubber;
        AudioManager.PlaybackStateChanged -= SetScrubberInteractableState;
    }

    // Diagnostic: This function takes <0.05ms on average per frame during song playback.
    private void UpdateSongScrubber()
    {
        disableNextUpdate = true;

        scrubber.value = (float)(SongTime.SongPositionSeconds / AudioManager.SongLength);
        
        // onValueChanged is still invocated when this function is called and will
        // cause a dual refresh on the same frame from UpdateSongTimeFromScrubber.
        // this doubles compute time needed and thus must be prevented.
    }

    private bool disableNextUpdate;

    private void UpdateSongTimeFromScrubber(float newPos)
    {
        if (disableNextUpdate)
        {
            disableNextUpdate = false;
            return;
        }

        var newTime = AudioManager.SongLength * scrubber.value;
        if (SongTime.SongPositionSeconds == newTime) return;

        SongTime.SongPositionSeconds = newTime;
        Chart.InPlaceRefresh();
    }

    private SortedDictionary<int, SectionData> representedData;
    
    private void Update()
    {
        // FIXME: Make this method of checking for updates to the SectionInstrument more efficient
        // This isn't horribly inefficient time-wise (0.2ms per frame during testing in the editor) but this could
        // ABSOLUTELY be made better. This is my "It's good enough" moment. 
        // If you plan on improving this, a good method would be to make an event that tracks changes in SectionInstrument's
        // LaneSet. There should be infrastructure in LaneSet that supports this already, but it might be a little rickety,
        // since as of writing this, I have not touched it in a while (as it was a failed approach to trying to update
        // HOPOs in FiveFretInstrument).
        var currentData = Chart.SectionInstrument.GetLaneData().ExportData();
        
                                                                                      // Not sure why JetBrains thinks this is a big deal - another fix here
        if (representedData is not null && representedData.Count == currentData.Count && representedData.SequenceEqual(currentData)) return;
        
        representedData = Chart.SectionInstrument.GetLaneData().ExportData();
        
        foreach (var sectionKVP in representedData)
        {

            var songRatio = sectionKVP.Key / (float)SongTime.SongLengthTicks;

            // Use sliding area because the SongScrubber game object does not have the same bounds as the actual scrubber
            // object. Since the song position is the same as the center line of the song scrubber object, there is
            // some dead space at the end of the scrubber where sections do not apply. The sliding area does not have
            // this. It looks a little weird but only if you work closely with it. To the untrained eye it looks fine.
            var yPosition = slidingArea.rect.height * songRatio;

            var sideSection = Instantiate(sideSectionPrefab, slidingArea).GetComponent<SideSection>();
            
            // FIXME: The hardcoded "10" is the left/right value shown in the inspector of the sliding area
            // I don't see this changing much so I didn't try to find the unity value for this. If this poses
            // an issue in the future then replace "10" with what represents the left/right offset in the inspector.
            sideSection.transform.localPosition = new Vector2(slidingArea.rect.width / 2 + 10, yPosition);
            sideSection.sectionName.text = sectionKVP.Value.Name;
        }
    }
}

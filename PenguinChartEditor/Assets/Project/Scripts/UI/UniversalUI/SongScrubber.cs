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
        SongTime.TimeChanged += UpdateSongScrubber;

        scrubber.onValueChanged.AddListener(x => UpdateSongTimeFromScrubber(x));
        AudioManager.PlaybackStateChanged += (playbackState =>
        {
            if (playbackState) scrubber.interactable = false;
            else scrubber.interactable = true;
        });

        var rect = GetComponent<RectTransform>();
        rectTransform = GetComponent<RectTransform>();
        print(rect.rect.center); // (0, 540)
        print(rect.rect.height); // 1080
        print(rect.rect.size); // (75, 1080)
        print(rect.pivot);
    }
    
    // Diagnostic: This function takes <0.05ms on average per frame during song playback.
    private void UpdateSongScrubber()
    {
        disableNextUpdate = true;

        scrubber.value = (float)SongTime.SongPositionSeconds / AudioManager.SongLength;
        
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
        var currentData = Chart.SectionInstrument.GetLaneData().ExportData();
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

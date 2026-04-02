using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PreviewTimeDropdown : MonoBehaviour
{
    private TMP_Dropdown attachedDropdown;
    private List<int> correspondingTicks;

    private int sectionDisplayCutoff => Chart.Resolution * 4;
    
    private void Awake()
    {
        Chart.ChartFileLoaded += OnEnable;
    }

    private void OnEnable()
    {
        attachedDropdown = GetComponent<TMP_Dropdown>();
        
        attachedDropdown.options.Clear();
        attachedDropdown.options.Add(new TMP_Dropdown.OptionData("--"));
        if (Chart.SectionInstrument is null) return;
        
        correspondingTicks = new List<int>(Chart.SectionInstrument.GetLaneData().Count + 1)
        {
            -1
        };

        foreach (var section in Chart.SectionInstrument.GetLaneData())
        {
            attachedDropdown.options.Add(new TMP_Dropdown.OptionData($"{section.Value.Name}"));
            correspondingTicks.Add(section.Key);
        }
        
        attachedDropdown.onValueChanged.AddListener(UpdatePreviewTimeFromDropdown);

        Chart.Metadata.PreviewStartTimeUpdated += UpdateSectionFromPreviewChange;
        UpdateSectionFromPreviewChange();
    }

    private void OnDisable()
    {
        Chart.Metadata.PreviewStartTimeUpdated -= UpdateSectionFromPreviewChange;
    }

    private void UpdatePreviewTimeFromDropdown(int option)
    {
        Chart.Metadata.PreviewStartTime = (float)Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(correspondingTicks[option]);
    }

    private void UpdateSectionFromPreviewChange()
    {
        var tick = Chart.SyncTrackInstrument.ConvertSecondsToTickTime(Chart.Metadata.PreviewStartTime);

        var tickPosition = correspondingTicks.BinarySearch(tick);

        if (tickPosition > 0) attachedDropdown.SetValueWithoutNotify(tickPosition);
        else
        {
            tickPosition = ~tickPosition;
            
            var nextTick = tickPosition < correspondingTicks.Count ? correspondingTicks[tickPosition] : int.MaxValue;
            var prevTick = tickPosition > 0 ? correspondingTicks[tickPosition - 1] : int.MinValue;

            var deltaToNext = nextTick - tick;
            var deltaToPrev = tick - prevTick;

            if (deltaToPrev < deltaToNext)
            {
                if (deltaToPrev < sectionDisplayCutoff)
                {
                    attachedDropdown.SetValueWithoutNotify(tickPosition - 1);
                }
                else
                {
                    attachedDropdown.SetValueWithoutNotify(0);
                }
            }
            else
            {
                if (deltaToNext < sectionDisplayCutoff)
                {
                    attachedDropdown.SetValueWithoutNotify(tickPosition);
                }
                else
                {
                    attachedDropdown.SetValueWithoutNotify(0);
                }
            }
        }
    }
}
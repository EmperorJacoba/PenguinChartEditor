using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Clap : MonoBehaviour
{
    public static bool clapActive = false;
    [SerializeField] private Button button;

    private void Awake()
    {
        button.onClick.AddListener(ToggleClap);
    }

    private void ToggleClap()
    {
        clapActive = !clapActive;

        if (clapActive)
        {
            SongTime.TimeChanged += CheckForClapHit;
        }
        else
        {
            SongTime.TimeChanged -= CheckForClapHit;
            firstLoop = true;
        }
    }

    private static int nextPromisedClapHit = -1;
    private bool firstLoop = true;
    private List<int> cachedTicks;

    private void CheckForClapHit()
    {
        // might change in case metronome for ffw/rw buttons is a wanted feature
        if (!AudioManager.AudioPlaying || !clapActive)
        {
            firstLoop = true;
            return;
        }

        if (firstLoop)
        {
            cachedTicks = Chart.LoadedInstrument.GetUniqueTickSet();

            nextPromisedClapHit = GetNextClipHit();

            firstLoop = false;
        }

        if (nextPromisedClapHit == -1) return;

        if (SongTime.SongPositionTicks >= nextPromisedClapHit)
        {
            AudioManager.PlayClapSound();

            nextPromisedClapHit = GetNextClipHit();
        }
    }

    private int GetNextClipHit()
    {
        var listIndex = cachedTicks.BinarySearch(SongTime.SongPositionTicks);

        if (listIndex < 0)
        {
            listIndex = ~listIndex;
        }

        if (listIndex >= cachedTicks.Count - 1) return -1;
        return cachedTicks[listIndex];
    }
}
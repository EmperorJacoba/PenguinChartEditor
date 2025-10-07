using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Lane : MonoBehaviour
{
    public SortedDictionary<int, IEventData> eventReference;
    public void UpdateEvents()
    {
        var eventsToDisplay = eventReference.Keys.Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();
        foreach (var @event in eventsToDisplay)
        {
            // wake up prefab from a pooler
            // tell that prefab to update its position based on the tick
            // tell that prefab to update the data it shows based on the value
        }
    }
}
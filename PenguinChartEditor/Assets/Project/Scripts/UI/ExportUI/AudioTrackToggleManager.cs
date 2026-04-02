using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioTrackToggleManager : MonoBehaviour
{
    [SerializeField] private GameObject audioTrackTogglePrefab;
    [SerializeField] private Transform scrollViewTransform;
    private List<AudioTrackToggle> activeTrackToggles = new List<AudioTrackToggle>();

    private void Start()
    {
        foreach (var stem in Chart.Metadata.StemPaths)
        {
            var trackToggle = Instantiate(audioTrackTogglePrefab, scrollViewTransform).GetComponent<AudioTrackToggle>();
            trackToggle.Initialize(stem.Key);
            print(stem.Key);
            activeTrackToggles.Add(trackToggle);
        }
    }

    public Dictionary<StemType, bool> GetTrackInclusionStatuses() =>
        activeTrackToggles.ToDictionary(x => x.audioStemType, x => x.toggle.isOn);
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioTrackToggleManager : MonoBehaviour
{
    private static AudioTrackToggleManager instance;
    [SerializeField] private GameObject audioTrackTogglePrefab;
    [SerializeField] private Transform scrollViewTransform;
    private List<AudioTrackToggle> activeTrackToggles = new List<AudioTrackToggle>();

    private void Start()
    {
        instance = this;
        foreach (var stem in Chart.Metadata.StemPaths)
        {
            var trackToggle = Instantiate(audioTrackTogglePrefab, scrollViewTransform).GetComponent<AudioTrackToggle>();
            trackToggle.Initialize(stem.Key);
            print(stem.Key);
            activeTrackToggles.Add(trackToggle);
        }
    }

    public static Dictionary<StemType, bool> GetTrackInclusionStatuses() =>
        instance.activeTrackToggles.ToDictionary(x => x.audioStemType, x => x.toggle.isOn);
}
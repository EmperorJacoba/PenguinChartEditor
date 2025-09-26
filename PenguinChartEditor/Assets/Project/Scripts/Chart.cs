using System;
using UnityEditor.SearchService;
using UnityEngine;

public class Chart : MonoBehaviour
{
    static Metadata Metadata { get; set; } = new();
    static Chart instance;

    public enum TabType
    {
        SongSetup,
        TempoMap
    }
    public static TabType currentTab;

    /// <summary>
    /// Number of ticks per quarter note (VERY IMPORTANT FOR SONG RENDERING)
    /// </summary>
    public static int Resolution
    {
        get
        {
            return _chartResolution;
        }
        set
        {
            if (value == 0) throw new ArgumentException("Resolution cannot be zero!");
            _chartResolution = value;
        }
    }
    private static int _chartResolution = 0;

    public static string FolderPath { get; private set; } = "";
    public static string ChartPath { get; private set; } = "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/Perpetual Change.chart";
    public static string IniPath { get; private set; } = "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/song.ini";

    public void SaveFile()
    {

    }

    public void LoadFile()
    {

    }

    void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
        }

        instance = this;
        DontDestroyOnLoad(instance);

        Metadata.TempSetUpStemDict();

        ChartParser chartParser = new(ChartPath);

        Resolution = chartParser.resolution;
        Metadata = chartParser.metadata;

        BPM.EventData.Events = chartParser.bpmEvents;
        TimeSignature.EventData.Events = chartParser.tsEvents;

        if (BPM.EventData.Events.Count == 0) // if there is no data to load in 
        {
            BPM.EventData.Events.Add(0, new BPMData(120.0f, 0)); // add placeholder bpm
        }
        if (TimeSignature.EventData.Events.Count == 0)
        {
            TimeSignature.EventData.Events.Add(0, new TSData(4, 4));
        }
    }

    public static void Refresh()
    {
        switch (currentTab)
        {
            case TabType.SongSetup:
                Debug.LogWarning("Song setup tab does not have an update function.");
                break;
            case TabType.TempoMap:
                TempoManager.UpdateBeatlines();
                break;
        }
    }
}
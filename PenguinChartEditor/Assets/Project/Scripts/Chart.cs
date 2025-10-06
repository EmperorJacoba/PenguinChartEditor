using System;
using System.Collections.Generic;
using UnityEngine;

public class Chart : MonoBehaviour
{
    public static Metadata Metadata { get; set; } = new();
    public static List<IInstrument> Instruments { get; set; }
    static Chart instance;
    public static void Log(string x) => Debug.Log(x);

    public enum TabType
    {
        SongSetup,
        TempoMap
    }
    public static TabType currentTab;

    public enum InstrumentType
    {
        guitar,
        coopGuitar,
        rhythm,
        bass,
        keys,
        drums,
        ghlGuitar,
        ghlBass,
        ghlRhythm,
        vox
    }

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

    public static string FolderPath { get; private set; } = "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change";
    public static string ChartPath { get; private set; } = "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/Perpetual Change.chart";

    public void SaveFile()
    {

    }

    public void LoadFile()
    {

    }

    void Awake()
    {
        // Only ever one chart game object active
        if (instance)
        {
            Destroy(gameObject);
        }

        instance = this;
        DontDestroyOnLoad(instance);

        ChartParser chartParser = new(ChartPath);

        Resolution = chartParser.resolution;
        Metadata = chartParser.metadata;
        Metadata.TempSetUpStemDict();

        BPM.EventData.Events = chartParser.bpmEvents;
        TimeSignature.EventData.Events = chartParser.tsEvents;

        Instruments = chartParser.instruments;

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
                BeatlinePreviewer.instance.UpdatePreviewPosition();
                break;
        }
    }
}
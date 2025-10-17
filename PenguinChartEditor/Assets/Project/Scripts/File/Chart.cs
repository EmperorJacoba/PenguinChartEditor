using System;
using System.Collections.Generic;
using UnityEngine;

public class Chart : MonoBehaviour
{
    public static Metadata Metadata { get; set; } = new();
    public static List<IInstrument> Instruments { get; set; }
    static Chart instance;
    public static void Log(string x) => Debug.Log(x);

    public static bool editMode = true;

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

    public enum DifficultyType
    {
        easy,
        medium,
        hard,
        expert
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

        Tempo.Events = chartParser.bpmEvents;
        TimeSignature.Events = chartParser.tsEvents;

        Instruments = chartParser.instruments;

        if (Tempo.Events.Count == 0) // if there is no data to load in 
        {
            Tempo.Events.Add(0, new BPMData(120.0f, 0)); // add placeholder bpm
        }
        if (TimeSignature.Events.Count == 0)
        {
            TimeSignature.Events.Add(0, new TSData(4, 4));
        }

        AudioManager.PlaybackStateChanged += x => { editMode = !AudioManager.AudioPlaying; };
    }

    public static void Refresh()
    {
        switch (currentTab)
        {
            case TabType.SongSetup:
                Debug.LogWarning("Song setup tab does not have an update function.");
                break;
            case TabType.TempoMap:
                BeatlineLane.instance.UpdateEvents();
                BPMLane.instance.UpdateEvents();
                TSLane.instance.UpdateEvents();
                break;
        }
    }
}
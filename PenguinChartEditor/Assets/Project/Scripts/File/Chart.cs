using System;
using System.Collections.Generic;
using UnityEngine;
using SFB;
using System.IO;

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

    public static string FolderPath { get; private set; }
    public static string ChartPath { get; private set; }

    public void SaveFile()
    {

    }

    public void LoadFile()
    {
        ChartPath = StandaloneFileBrowser.OpenFilePanel($"Open .chart file to load from.", "", new[] { new ExtensionFilter(".chart files ", "chart") }, false)[0];
        FolderPath = ChartPath[..ChartPath.LastIndexOf("\\")];
        

        ChartParser chartParser = new(ChartPath);

        Resolution = chartParser.resolution;
        Metadata = chartParser.metadata;
        
        foreach (StemType key in Enum.GetValues(typeof(StemType)))
        {
            string targetFilePath = $"{FolderPath}/{key}.opus";
            if (File.Exists(targetFilePath))
            {
                Metadata.StemPaths.Add(key, targetFilePath);
            }
        }

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

        LoadFile();

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
using UnityEngine;
using System;
using System.Collections.Generic;
using SFB;
using System.IO;
using System.Linq;

public class Chart : MonoBehaviour
{
    public static Chart instance;

    // Use this for scene-related generic calculations
    // Add code to reassign this variable upon scene change
    public SceneDetails SceneDetails
    {
        get
        {
            if (sceneDetails == null)
            {
                throw new ArgumentException(
                    "Please create and assign a SceneDetails object in this scene. A SceneDetails object is required for selections and moving."
                    );
            }
            return sceneDetails;
        }
    }
    [SerializeField] SceneDetails sceneDetails;

    public static void Log(string x) => Debug.Log(x); // debug shortcut for static classes like parsers

    #region Chart Data

    public static Metadata Metadata { get; set; } = new();
    public static List<IInstrument> Instruments { get; set; }
    public static IInstrument LoadedInstrument { get; set; }
    public static T GetActiveInstrument<T>() where T : IInstrument => (T)LoadedInstrument;
    public static SyncTrackInstrument SyncTrackInstrument { get; set; }

    #endregion

    #region Modify Chart Data

    public void SaveFile()
    {
        ChartWriter.WriteChart();
    }

    public void LoadFile()
    {
        var pathCandidates = StandaloneFileBrowser.OpenFilePanel($"Open .chart file to load from.", "", new[] { new ExtensionFilter(".chart files ", "chart") }, false);

        ChartPath = pathCandidates[0];
        FolderPath = ChartPath[..ChartPath.LastIndexOf("\\")];

        ChartParser chartParser = new(ChartPath);

        Resolution = chartParser.resolution;
        Metadata = chartParser.metadata;

        // testing: please add audio selection in future if excess audio files are found
        foreach (StemType key in Enum.GetValues(typeof(StemType)))
        {
            string targetFilePath = $"{FolderPath}/{key}.opus";
            if (File.Exists(targetFilePath))
            {
                Metadata.StemPaths.Add(key, targetFilePath);
            }
        }

        AudioManager.InitializeAudio();

        SyncTrackInstrument = new(chartParser.bpmEvents, chartParser.tsEvents);

        Instruments = chartParser.instruments;
        foreach (var instrument in Instruments)
        {
            instrument.SetUpInputMap();
        }

        if (SyncTrackInstrument.TempoEvents.Count == 0) // if there is no data to load in 
        {
            SyncTrackInstrument.TempoEvents.Add(0, new BPMData(120.0f, 0, false)); // add placeholder bpm
        }
        if (SyncTrackInstrument.TimeSignatureEvents.Count == 0)
        {
            SyncTrackInstrument.TimeSignatureEvents.Add(0, new TSData(4, 4));
        }
    }

    #endregion

    #region Chart Properties

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
    public static string ChartPath
    {
        get
        {
            if (_chPath == null)
            {
                var name = Metadata.SongInfo[Metadata.MetadataType.name];
                var artist = Metadata.SongInfo[Metadata.MetadataType.artist];
                _chPath = FolderPath + $"\\{artist} - {name}.chart";
            }
            return _chPath;
        }
        private set
        {
            _chPath = value;
        }
    }
    static string _chPath;

    public enum TabType
    {
        SongSetup,
        TempoMap,
        Chart
    }
    public static TabType currentTab;

    #endregion

    public static bool showPreviewers = true; // for previewers

    InputMap inputMap;

    void Awake()
    {
        // Only ever one chart game object active, prioritize first loaded
        if (instance != null)
        {
            Destroy(gameObject);
        }

        instance = this;
        DontDestroyOnLoad(instance);

        LoadFile();
        LoadedInstrument = Instruments.
            Where(
            item => item.InstrumentName == InstrumentType.guitar). // for testing only
            ToList()[0];
        // LoadedInstrument = SyncTrackInstrument;

        AudioManager.PlaybackStateChanged += x => { showPreviewers = !AudioManager.AudioPlaying; };

        inputMap = new();
        inputMap.Enable();
        inputMap.Charting.Copy.performed += x => Clipboard.Copy();
        inputMap.Charting.Paste.performed += x => Clipboard.Paste();
        inputMap.Charting.Cut.performed += x => Clipboard.Cut();
    }

    public delegate void ChartUpdatedDelegate();
    public static event ChartUpdatedDelegate ChartTabUpdated;

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
            case TabType.Chart:
                BeatlineLane3D.instance.UpdateEvents();
                ChartTabUpdated?.Invoke(); // shortcut for all lanes to update
                break;
        }
    }
    public static int hopoCutoff
    {
        get
        {
            return (int)Math.Floor(((float)65 / 192) * (float)Resolution);
        }
    }

    public enum SelectionMode
    {
        dynamic,
        select,
        edit,
        view
    }
    public static SelectionMode currentSelectionMode = SelectionMode.dynamic;
    public static bool IsSelectionAllowed()
    {
        return currentSelectionMode switch
        {
            SelectionMode.dynamic => true,
            SelectionMode.select => true,
            SelectionMode.edit => false,
            SelectionMode.view => false,
            _ => throw new ArgumentException("Invalid assigned selection mode."),
        };
    }

    public static bool IsEditAllowed()
    {
        return currentSelectionMode switch
        {
            SelectionMode.dynamic => true,
            SelectionMode.select => false,
            SelectionMode.edit => true,
            SelectionMode.view => false,
            _ => throw new ArgumentException("Invalid assigned selection mode.")
        };
    }
}
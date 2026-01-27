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
            if (!sceneDetails)
            {
                throw new ArgumentException(
                    "Please create and assign a SceneDetails object in this scene. A SceneDetails object is required for selections and moving."
                    );
            }
            return sceneDetails;
        }
    }
    [SerializeField] private SceneDetails sceneDetails;

    public static void Log(string x) => Debug.Log(x); // debug shortcut for static classes like parsers

    #region Chart Data

    public static Metadata Metadata { get; set; } = new();
    public static List<IInstrument> Instruments { get; set; }
    public static IInstrument LoadedInstrument { get; set; }

    public static ISustainableInstrument LoadedSustainableInstrument
    {
        get
        {
            if (LoadedInstrument is ISustainableInstrument sustainedInstrument) return sustainedInstrument;
            else
                throw new ArgumentException(
                    "You are trying to access properties only applicable to sustainable instruments on an instrument " +
                    "that has not been set up to support sustains. " +
                    "Please fix the instrument or remove the reference to an ISustainableInstrument."
                    );
        }
    }
    public static T GetActiveInstrument<T>() where T : IInstrument => (T)LoadedInstrument;
    public static SyncTrackInstrument SyncTrackInstrument { get; set; }
    public static StarpowerInstrument StarpowerInstrument { get; set; }

    #endregion

    #region Modify Chart Data

    public void SaveFile()
    {
        ChartWriter.WriteChart();
    }

    public static void ApplyFileInformation(
        Metadata metadata,
        List<IInstrument> traditionalInstruments,
        SyncTrackInstrument syncTrack,
        StarpowerInstrument starpower
        )
    {
        Metadata = metadata;
        Instruments = traditionalInstruments;
        SyncTrackInstrument = syncTrack;
        StarpowerInstrument = starpower;

        SyncTrackInstrument.SetUpInputMap();
        StarpowerInstrument.SetUpInputMap();
        foreach (var instrument in Instruments)
        {
            instrument.SetUpInputMap();
        }   
    }

    public static void LoadFile()
    {
        var pathCandidates = StandaloneFileBrowser.OpenFilePanel($"Open .chart file to load from.", "", new[] { new ExtensionFilter(".chart files ", "chart") }, false);

        ChartPath = pathCandidates[0];
        FolderPath = Path.GetDirectoryName(ChartPath);

        ChartParser.ParseChart(ChartPath);

        // also need to parse chart stems
        // find properly named files - add to stems
        // find other audio files - ask to assign
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
        Waveform.InitializeWaveformData();
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
            return _chartRes == -1 ? throw new ArgumentException("Uninitialized resolution.") : _chartRes;
        }
        set
        {
            if (value == 0) throw new ArgumentException("Resolution cannot be zero!");
            _chartRes = value;
            _cachcut = (int)Math.Floor(((float)65 / 192) * (float)_chartRes);
        }
    }
    private static int _chartRes = -1;

    public static int HopoCutoff
    {
        get
        {
            return _cachcut == -1 ? throw new ArgumentException("Uninitialized hopo cutoff.") : _cachcut;
        }
    }

    private static int _cachcut = -1;

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
    private static string _chPath;

    #endregion

    public static bool showPreviewers = true;

    private InputMap inputMap;

    private void Awake()
    {
        // Only ever one chart game object active, prioritize first loaded
        if (instance != null)
        {
            Destroy(gameObject);
        }

        instance = this;
        DontDestroyOnLoad(instance);

        AudioManager.InitializeBassPlugin();

        LoadFile();
        
        // LoadedInstrument = Instruments.Where(item => item.InstrumentName == InstrumentType.guitar).ToList()[0]; 
        // LoadedInstrument = SyncTrackInstrument;
        LoadedInstrument = StarpowerInstrument;

        inputMap = new InputMap();
        inputMap.Enable();
        inputMap.Charting.Copy.performed += _ => Clipboard.Copy();
        inputMap.Charting.Paste.performed += _ => Clipboard.Paste();
        inputMap.Charting.Cut.performed += _ => Clipboard.Cut();
    }

    public delegate void InPlaceUpdatedDelegate();
    public static event InPlaceUpdatedDelegate InPlaceRefreshNeeded;

    /// <summary>
    /// When BPM events change, the time value of a tick changes, so the waveform must refresh to update the cached info in waveform that dictates event spawning.
    /// </summary>
    public static void SyncTrackInPlaceRefresh()
    {
        Waveform.GenerateWaveformPoints();
        InPlaceRefreshNeeded?.Invoke();
    }

    public static void InPlaceRefresh()
    {
        InPlaceRefreshNeeded?.Invoke(); // shortcut for all lanes to update
    }

    public enum SelectionMode
    {
        Edit,
        Select,
        View
    }
    public static SelectionMode currentSelectionMode = SelectionMode.Edit;
    public static bool IsSelectionAllowed()
    {
        return currentSelectionMode switch
        {
            SelectionMode.Select => true,
            SelectionMode.Edit => false,
            SelectionMode.View => false,
            _ => throw new ArgumentException("Invalid assigned selection mode."),
        };
    }

    public static bool IsPlacementAllowed()
    {
        return currentSelectionMode switch
        {
            SelectionMode.Select => false,
            SelectionMode.Edit => true,
            SelectionMode.View => false,
            _ => throw new ArgumentException("Invalid assigned selection mode.")
        };
    }

    public static bool IsModificationAllowed()
    {
        return currentSelectionMode switch
        {
            SelectionMode.Select => true,
            SelectionMode.Edit => true,
            SelectionMode.View => false,
            _ => throw new ArgumentException("Invalid assigned selection mode.")
        };
    }
}
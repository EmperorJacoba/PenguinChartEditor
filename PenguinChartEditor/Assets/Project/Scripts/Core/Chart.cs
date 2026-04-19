using UnityEngine;
using System;
using System.Collections.Generic;
using SFB;
using System.IO;
using System.Linq;

public class Chart : MonoBehaviour
{
    public static Chart instance;

    [SerializeField] private bool isDebug;
    [SerializeField] private HeaderType DebugLoadedInstrument = (HeaderType)(-1);

    private void Start()
    {
        if (DebugLoadedInstrument != (HeaderType)(-1) && !TabSceneSpawningManager.IsTabbingActive())
        {
            SetLoadedInstrument(DebugLoadedInstrument);
        }
    }

    #region SceneDetails
    
    // Use this for scene-related generic calculations
    [SerializeField] private SceneDetails sceneDetails;
    public static void SetSceneDetails(SceneDetails @new) => instance.sceneDetails = @new; 
    public static bool IsSceneOverlayUIHit() => instance.sceneDetails is not null && instance.sceneDetails.IsSceneOverlayUIHit();
    public static bool IsEventDataHit() => instance.sceneDetails is not null && instance.sceneDetails.IsEventDataHit();
    public static float GetCursorHighwayProportion() => instance.sceneDetails?.GetCursorHighwayProportion() ?? 0.0f;
    public static Vector3 GetCursorHighwayPosition() => instance.sceneDetails?.GetCursorHighwayPosition() ?? Vector3.zero;

    #endregion
    
    #region Chart Data

    public static Metadata Metadata { get; private set; } = new();
    public static List<IInstrument> Instruments { get; set; }

    public static bool IsInstrumentCreated(HeaderType instrumentID)
    {
        return Instruments.Any(x => x.InstrumentID == instrumentID);
    }

    public static bool IsInstrumentCreated(HeaderType instrumentID, out IInstrument instrument)
    {
        instrument = null;

        switch (instrumentID)
        {
            case HeaderType.SyncTrack:
                instrument = SyncTrackInstrument;
                return true;
            case HeaderType.Starpower:
                instrument = StarpowerInstrument;
                return true;
            case HeaderType.Events:
                instrument = SectionInstrument;
                return true;
        }
        
        var result = Instruments.Where(x => x.InstrumentID == instrumentID);

        if (!result.Any()) return false;
        
        instrument = result.First();
        return true;
    }
    
    public static HashSet<InstrumentType> GetLoadedInstrumentTypes()
    {
        HashSet<InstrumentType> outputSet = new();
        foreach (var entry in Instruments)
        {
            outputSet.Add(entry.InstrumentName);
        }

        return outputSet;
    }

    public static void DuplicateInstrumentToNewDifficulty(HeaderType originalID, HeaderType newID)
    {
        if (!IsInstrumentCreated(originalID, out var instrument))
        {
            return;
        }
        Instruments.Add(instrument.DuplicateToNewInstrument(newID));
    }

    public static List<ActiveInstrument> GetInstrumentDifficultyInformation()
    {
        HashSet<InstrumentType> foundInstruments = new();
        List<ActiveInstrument> instrumentData = new();

        foreach (var instrument in Instruments)
        {
            var name = instrument.InstrumentName;
            if (!foundInstruments.Add(name))
            {
                var instrumentDataObj = instrumentData.First(x => x.name == name);
                instrumentDataObj.activeDifficulties.Add(instrument.Difficulty);
            }
            else
            {
                instrumentData.Add(new ActiveInstrument(name, instrument.Difficulty));
            }
        }

        instrumentData = instrumentData.OrderBy(x => (int)x.name).ToList();
        return instrumentData;
    }

    public static HashSet<DifficultyType> GetActiveDifficulties(InstrumentType instrumentType)
    {
        return Instruments.Where(x => x.InstrumentName == instrumentType).Select(x => x.Difficulty)
            .ToHashSet();
    }

    public static HashSet<HeaderType> GetUniqueLoadedInstruments()
    {
        HashSet<HeaderType> outputSet = new();
        foreach (var entry in Instruments)
        {
            outputSet.Add(entry.InstrumentID);
        }

        return outputSet;
    }
    
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
    
    // These "instruments" are distinguished because they are not really instruments. They are important non-traditional instruments
    // that are accessed frequently and outside of LoadedInstrument enough to be distinguished.
    public static SyncTrackInstrument SyncTrackInstrument { get; private set; }
    public static StarpowerInstrument StarpowerInstrument { get; private set; }
    public static SectionInstrument SectionInstrument { get; private set; }

    #endregion

    #region Modify Chart Data

    public void SaveFile()
    {
    }

    public static void ApplyFileInformation(
        Metadata metadata,
        List<IInstrument> traditionalInstruments,
        SyncTrackInstrument syncTrack,
        StarpowerInstrument starpower,
        SectionInstrument sections
        )
    {
        Metadata = metadata;
        Instruments = traditionalInstruments;
        SyncTrackInstrument = syncTrack;
        StarpowerInstrument = starpower;
        SectionInstrument = sections;

        SyncTrackInstrument.SetUpInputMap();
        StarpowerInstrument.SetUpInputMap();
        SectionInstrument.SetUpInputMap();
        
        foreach (var instrument in Instruments)
        {
            instrument.SetUpInputMap();
        }   
    }

    public delegate void ChartFileLoadedDel();

    public static event ChartFileLoadedDel ChartFileLoaded;

    public static bool ChartLoading { get; private set; }
    
    public static bool LoadFile()
    {
        var pathCandidates = 
            StandaloneFileBrowser.OpenFilePanel
                (
                    $"Open .chart file to load from.", 
                    "", 
                    new[]
                    {
                        new ExtensionFilter(
                            ".chart files ", 
                            "chart")
                    }, 
                    false
                );

        if (pathCandidates.Length < 1) return false;
        
        ChartPath = pathCandidates[0];
        FolderPath = Path.GetDirectoryName(ChartPath);

        ChartLoading = true;

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

        AudioManager.RefreshAudioStreams();
        Waveform.InitializeWaveformData();

        ChartLoading = false;
        ChartFileLoaded?.Invoke();

        return true;
    }

    #endregion

    // Currently written for .chart exclusively, rework for other formats later
    public static void ExportFile(string targetDirectory)
    {
        // need to export chart, image, background, ini, audio
        // export everything to temp directory and then either copy the directory's contents to target,
        // or compress as zip and put to target

        var exportSettingsManager = ExportSettingsManager.instance;
        
        if (exportSettingsManager is null)
        {
            Debug.LogError($"No export settings to read from. Aborting export operation.");
            return;
        }
        
        Directory.CreateDirectory(
            // weird preceding thing is to fix errors resulting from long file paths? apparently?
            // it works so i am not touching it
            Path.Combine(@"\\?\", targetDirectory) 
            );
        
        IniWriter.WriteIni(targetDirectory, Metadata);

        var allInstruments = new List<IInstrument>()
        {
            SyncTrackInstrument,
            SectionInstrument
        };
        allInstruments.AddRange(Instruments);
        
        ChartWriter.WriteChart(
            targetDirectory: targetDirectory,
            resolution: Resolution,
            metadata: Metadata,
            instruments: allInstruments,
            includedTracks: exportSettingsManager.GetInstrumentTrackInclusionStatuses(),
            audioFormat: exportSettingsManager.GetExportAudioFormat()
            );
        
        AudioManager.WriteAudioFiles(
            Metadata, 
            targetDirectory, 
            exportSettingsManager.GetExportAudioFormat(), 
            exportSettingsManager.GetAudioInclusionStatuses(), 
            exportSettingsManager.GetKBPS()
            );

        if (File.Exists(Metadata.BackgroundPath))
        {
            File.Copy(Metadata.BackgroundPath, $"{targetDirectory}/background.jpg");
        }

        if (File.Exists(Metadata.CoverPath))
        {
            File.Copy(Metadata.CoverPath, $"{targetDirectory}/album.jpg");
        }
    }

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
            // From .chart specifications (hopo cutoff). This is cached because this is frequently used.
            _cachcut = (int)Math.Floor((65.0f / 192.0f) * _chartRes);
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
            return;
        }
        instance = this;
        DontDestroyOnLoad(instance);

        AudioManager.Initialize();

        if (isDebug)
        {
            if (!LoadFile())
            {
                Debug.Break();
            }
        }
        
        SetUpInputMap();
    }

    private void SetUpInputMap()
    {
        inputMap = new InputMap();
        inputMap.Enable();
        inputMap.Charting.Copy.performed += _ => Clipboard.Copy();
        inputMap.Charting.Paste.performed += _ => Clipboard.Paste();
        inputMap.Charting.Cut.performed += _ => Clipboard.Cut();
    }

    private void OnDestroy()
    {
        inputMap?.Disable();
    }

    public static void SetLoadedInstrument(InstrumentType instrumentName, DifficultyType difficulty)
    {
        var id = (int)instrumentName + (int)difficulty;
        SetLoadedInstrument((HeaderType)id);
    }
    
    public static void SetLoadedInstrument(HeaderType instrumentID)
    {
        switch (instrumentID)
        {
            case HeaderType.SyncTrack:
            {
                LoadedInstrument = SyncTrackInstrument;
                break;
            }
            case HeaderType.Starpower:
            {
                LoadedInstrument = StarpowerInstrument;
                break;
            }
            case HeaderType.Events:
            {
                LoadedInstrument = SectionInstrument;
                break;
            }
            default:
            {
                var foundInstruments = Instruments.Where(item => item.InstrumentID == instrumentID).ToList();
                if (foundInstruments.Count > 0)
                {
                    LoadedInstrument = foundInstruments[0];
                }
                else LoadedInstrument = CreateNewInstrument(instrumentID);

                break;
            }
        }
    }

    private static IInstrument CreateNewInstrument(HeaderType instrumentID)
    {
        switch (InstrumentMetadata.GetInstrumentGroup(instrumentID))
        {
            case InstrumentCategory.FiveFret:
                return new FiveFretInstrument(instrumentID, new List<KeyValuePair<int, string>>());
            case InstrumentCategory.FourLaneDrums:
            case InstrumentCategory.EliteDrums:
            case InstrumentCategory.GHL:
            case InstrumentCategory.Vox:
            default:
                throw new ArgumentOutOfRangeException($"No support for creating instrument type {instrumentID}");
        }
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
        if (SyncTrackInstrument is null)
        {
            print("Bounced attempted refresh as data has not yet been loaded. " +
                  "Please make sure you are calling refresh at the right time.");
            return;
        }

        if (LoadedInstrument == SyncTrackInstrument)
        {
            SyncTrackInPlaceRefresh();
            return;
        }
        
        InPlaceRefreshNeeded?.Invoke(); // shortcut for all lanes to update
    }
    
    #region Scene edit permissions

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
    
    #endregion
}
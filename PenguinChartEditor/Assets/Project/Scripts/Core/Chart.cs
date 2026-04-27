using UnityEngine;
using System;
using System.Collections.Generic;
using SFB;
using System.IO;
using System.Linq;

public class Chart : MonoBehaviour
{
    public static Chart instance;

    #region Instance Components
    
    [SerializeField] private bool isDebug;
    [SerializeField] private HeaderType DebugLoadedInstrument = (HeaderType)(-1);
    [SerializeField] private GameObject fileChangeDialog;

    #endregion
    
    #region SceneDetails
    
    // Use this for scene-related generic calculations
    [SerializeField] private SceneDetails sceneDetails;
    public static void SetSceneDetails(SceneDetails @new) => instance.sceneDetails = @new;

    public static bool IsSceneOverlayUIHit()
    {
        return
            instance.sceneDetails is not null &&
            (
                TabSceneSpawningManager.IsTabLayerHit() ||
                instance.sceneDetails.IsSceneOverlayUIHit()
            );
    }
    public static bool IsEventDataHit() => instance.sceneDetails is not null && instance.sceneDetails.IsEventDataHit();
    public static float GetCursorHighwayProportion() => instance.sceneDetails?.GetCursorHighwayProportion() ?? 0.0f;
    public static Vector3 GetCursorHighwayPosition() => instance.sceneDetails?.GetCursorHighwayPosition() ?? Vector3.zero;

    #endregion
    
    #region Instance Setup
    
    private void Start()
    {
        if (DebugLoadedInstrument != (HeaderType)(-1) && !TabSceneSpawningManager.IsTabbingActive())
        {
            SetLoadedInstrument(DebugLoadedInstrument);
        }
    }
    
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
            if (!InternalLoadFile())
            {
                Debug.Break();
            }
        }
        
        SetUpInputMap();
        
        InternalSaveFile();
    }

    private InputMap inputMap;
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
    
    #endregion

    #region Chart Data

    public static Metadata Metadata { get; private set; } = new();
    public static List<IInstrument> Instruments { get; set; }
    public static IInstrument LoadedInstrument { get; set; }
    
    // These "instruments" are distinguished because they are not really instruments. They are important non-traditional instruments
    // that are accessed frequently and outside of LoadedInstrument.
    public static SyncTrackInstrument SyncTrackInstrument { get; private set; }
    public static StarpowerInstrument StarpowerInstrument { get; private set; }
    public static SectionInstrument SectionInstrument { get; private set; }
    
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
    
    #endregion
    
    #region Chart Properties

    public static bool IsResolutionInitialized() => _chartRes != -1;

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

    public static string FolderPath { get; private set; } = "";
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
    
    public static bool showPreviewers = true;
    
    #endregion

    #region Instrument Queries/Changes
    
    public static void SetLoadedInstrument(InstrumentType instrumentName, DifficultyType difficulty)
    {
        SetLoadedInstrument(
            InstrumentMetadata.GetHeader(instrumentName, difficulty)
        );
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

    public static List<IInstrument> CompileAllInstruments()
    {
        var allInstruments = new List<IInstrument>()
        {
            SyncTrackInstrument,
            StarpowerInstrument,
            SectionInstrument
        };
        allInstruments.AddRange(Instruments);
        return allInstruments;
    }
    
    public static bool IsInstrumentCreated(HeaderType instrumentID) => 
        CompileAllInstruments().Any(x => x.InstrumentID == instrumentID);

    public static bool IsInstrumentCreated(HeaderType instrumentID, out IInstrument instrument)
    {
        instrument = null;
        var result = CompileAllInstruments().Where(x => x.InstrumentID == instrumentID);

        if (!result.Any()) return false;
        
        instrument = result.First();
        return true;
    }
    
    public static HashSet<InstrumentType> GetLoadedTraditionalInstrumentTypes()
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
    
    #endregion

    #region Save/New/Delete
    
    public static void LoadFile() => instance.PromptDelete(() => InternalLoadFile());
    public static void NewFile() => instance.PromptDelete(InternalNewFile);
    public static void SaveFile() => instance.PromptDelete(InternalSaveFile);
    public static void SaveFileAs() => instance.PromptDelete(InternalSaveFileAs);
    
    public bool saved = true;

    private void PromptDelete(Action resultantAction)
    {
        if (isDebug)
        {
            saved = true;
        }

        if (saved) resultantAction();
        
        var dialog = Instantiate(fileChangeDialog, TabSceneSpawningManager.instance.canvas.transform).GetComponent<DataWipeDialog>();
        dialog.Initialize(
            "This action will delete all unsaved data. Save data before continuing?", 
            () => {
                SaveFile();
                resultantAction();
            },
            resultantAction
            );
    }


    private static void InternalSaveFile()
    {
        var initTime = Time.realtimeSinceStartup;
        PenguinWriter.WritePenguin(FolderPath, Metadata, CompileAllInstruments());
        print(Time.realtimeSinceStartup - initTime);
    }
    
    private static void InternalNewFile()
    {
        var pathCandidates = StandaloneFileBrowser.OpenFolderPanel(
            "Open folder to create new chart file in.",
            FolderPath,
            false
        );
        
        if (pathCandidates.Length < 1) return;

        FolderPath = pathCandidates[0];

        ChartLoading = true;
        
        ApplyFileInformation(new ChartFileInformation(
            new Metadata(), 
            new List<IInstrument>(), 
            new SyncTrackInstrument(), 
            new StarpowerInstrument(), 
            new SectionInstrument())
        );
        
        ChartLoading = false;
        ChartFileLoaded?.Invoke();
        
        SaveFile();
    }

    private static void InternalSaveFileAs()
    {
        
    }

    public delegate void ChartFileLoadedDel();

    public static event ChartFileLoadedDel ChartFileLoaded;

    public static bool ChartLoading { get; private set; }

    public static bool InternalLoadFile()
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

        ApplyFileInformation(ChartParser.ParseChart(ChartPath));

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
    
    private static void ApplyFileInformation(
        ChartFileInformation info
    )
    {
        Metadata = info.metadata;
        Instruments = info.traditionalInstruments;
        SyncTrackInstrument = info.syncTrack;
        StarpowerInstrument = info.starpower;
        SectionInstrument = info.sections;

        SyncTrackInstrument.SetUpInputMap();
        StarpowerInstrument.SetUpInputMap();
        SectionInstrument.SetUpInputMap();
        
        foreach (var instrument in Instruments)
        {
            instrument.SetUpInputMap();
        }   
    }
    
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
        
        ChartWriter.WriteChart(
            targetDirectory: targetDirectory,
            resolution: Resolution,
            metadata: Metadata,
            instruments: CompileAllInstruments(),
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
    
    #endregion
    
    #region Refresh

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
    
    #endregion
    
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

public class ChartFileInformation
{
    public Metadata metadata;
    public List<IInstrument> traditionalInstruments;
    public SyncTrackInstrument syncTrack;
    public StarpowerInstrument starpower;
    public SectionInstrument sections;

    public ChartFileInformation(Metadata metadata, List<IInstrument> traditionalInstruments, SyncTrackInstrument syncTrack, StarpowerInstrument starpower, SectionInstrument sections)
    {
        this.metadata = metadata;
        this.traditionalInstruments = traditionalInstruments;
        this.syncTrack = syncTrack;
        this.starpower = starpower;
        this.sections = sections;
    }
    
    public ChartFileInformation() {}
}
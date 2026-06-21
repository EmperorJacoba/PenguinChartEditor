using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SFB;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Penguin.Debug;
using Penguin.Dialogs;
using UnityEngine.SceneManagement;

/// <summary>
/// The central unit of Penguin. An instance of this class is guaranteed to exist at all times. Handles file I/O, various
/// events, spawning rules, etc.
/// </summary>
public class Chart : MonoBehaviour
{
    private static Chart instance;
    public static UserSettings settings;

    #region Instance Components
    
    [SerializeField] private bool isDebug;
    [SerializeField] private HeaderType DebugLoadedInstrument = (HeaderType)(-1);

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

        if (openWithFileError)
        {
            ShowLoadError();
        }
    }

    private static bool openWithFileError = false;
    
    // Effectively the entry point into Penguin. Make sure any unity object functions that depend on Chart data run in
    // Start(), not Awake(), to guarantee a call after Chart setup.
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
        
        settings = UserSettings.ReadFromDisk();

        Resolution = settings.DefaultResolution;
        AudioManager.Initialize();

        if (isDebug)
        {
            if (!InternalLoadFile())
            {
                Debug.Break();
            }
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (Environment.GetCommandLineArgs().Length > 1)
        {
            try
            {
                _InternalLoadFile(Environment.GetCommandLineArgs()[1]);
                SceneManager.LoadScene("ContainerSceneV2");
            }
            catch (Exception e)
            {
                Debug.Log($"Error when loading file with \"open with\".\n\t{e}");
                openWithFileError = true;
            }
        }
        else
#endif
            // for stability reasons as most rendering depends on this
            SyncTrackInstrument = new SyncTrackInstrument();

        SetUpInputMap();

        StartCoroutine(AutosaveRoutine());

        Application.wantsToQuit += AskForDataSave;
    }

    private IEnumerator AutosaveRoutine()
    {
        while (true)
        {
            Autosave();
            
            // temporary measure to stop wantsToQuit from bugging out
            saved = false;
            yield return new WaitForSeconds(5.0f);
        }
    }
    
    private void Autosave()
    {
        if (!saved && fileLoaded)
        {
            InternalSaveFile(false, Application.persistentDataPath, $"autosave-{Hash128.Compute(ChartPath)}");
        }
    }

    private InputMap inputMap;
    private void SetUpInputMap()
    {
        inputMap = new InputMap();
        inputMap.Enable();
        inputMap.StandardCommands.Copy.performed += _ => Clipboard.Copy();
        inputMap.StandardCommands.Paste.performed += _ => Clipboard.Paste();
        inputMap.StandardCommands.Cut.performed += _ => Clipboard.Cut();
        inputMap.StandardCommands.Save.performed += _ => SaveFile();
        inputMap.StandardCommands.SaveAs.performed += _ => SaveFileAs();
        inputMap.StandardCommands.New.performed += _ => NewFile();
        inputMap.StandardCommands.Open.performed += _ => LoadFile();
    }
    
    private void OnDestroy()
    {
        inputMap?.Disable();
    }

    private bool quitNextRound = false;
    /// <summary>
    /// Check to make sure changes are saved upon application quit.
    /// </summary>
    /// <returns>Can the program safely quit?</returns>
    private bool AskForDataSave()
    {
        settings.SaveSettingsToDisk();
        if (saved || quitNextRound) return true;
        
        var dialog = DialogManager.SpawnDialog<DataWipeDialog>();
        
        dialog.Initialize(
            "Save unsaved changes before exiting?",
            () =>
            {
                if (!InternalSaveFile(false)) return false;

                quitNextRound = true;
                Application.Quit();
                return true;
            },
            () =>
            {
                quitNextRound = true;
                Application.Quit();
                return false;
            });

        return false;
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
        get => _chartRes == -1 ? throw new ArgumentException("Uninitialized resolution.") : _chartRes;
        set
        {
            if (value == 0) throw new ArgumentException("Resolution cannot be zero!");
            _chartRes = value;
            
            // From .chart specifications (hopo cutoff). This is cached because this is frequently used.
            _cachcut = (int)Math.Floor((65.0f / 192.0f) * _chartRes);
        }
    }
    private static int _chartRes = -1;

    public static int HopoCutoff => _cachcut == -1 ? throw new ArgumentException("Uninitialized hopo cutoff.") : _cachcut;

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
                _chPath = FolderPath + $"\\{artist} - {name}.penguin";
            }
            return _chPath;
        }
        private set => _chPath = value;
    }
    private static string _chPath;

    public static string ChartName => Path.GetFileNameWithoutExtension(ChartPath);
    
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
            case HeaderType.Song:
            {
                LoadedInstrument = null;
                break;
            }
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
                else
                {
                    LoadedInstrument = CreateNewInstrument(instrumentID);
                }

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
        IInstrument createdInstrument;
        switch (InstrumentMetadata.GetInstrumentGroup(instrumentID))
        {
            case InstrumentCategory.FiveFret:
                createdInstrument = new FiveFretInstrument(instrumentID, new List<KeyValuePair<int, string>>());
                break;
            case InstrumentCategory.FourLaneDrums:
            case InstrumentCategory.EliteDrums:
            case InstrumentCategory.GHL:
            case InstrumentCategory.Vox:
            default:
                throw new ArgumentOutOfRangeException($"No support for creating instrument type {instrumentID}");
        }
        
        createdInstrument.SetUpInputMap();
        Instruments.Add(createdInstrument);
        return createdInstrument;
    }
    
    #endregion

    #region Save/New/Delete
    
    public static bool LoadFile() => instance.PromptDelete(InternalLoadFile);
    public static bool NewFile() => instance.PromptDelete(InternalNewFile);
    public static void SaveFile() => InternalSaveFile(true);
    public static void SaveFileAs() => InternalSaveFileAs();
    
    public static bool saved = false;
    public static bool fileLoaded = false;

    private bool PromptDelete(Func<bool> resultantAction)
    {
        if (isDebug)
        {
            saved = true;
        }

        if (saved || !fileLoaded)
        {
            return resultantAction();
        }

        var dialog = DialogManager.SpawnDialog<DataWipeDialog>();
        dialog.Initialize(
            "This action will delete all unsaved data. Save data before continuing?", 
            () => {
                SaveFile();
                resultantAction();
                return true;
            },
            resultantAction
            );
        
        return true;
    }

    private static bool InternalSaveFile(bool showSaved, string directory = null, string name = null)
    {
        try
        {
            return _InternalSaveFile(showSaved, directory, name);
        }
        catch (Exception e)
        {
            Debug.Log($"Error when saving file.\n\t{e}");
            var dialog = DialogManager.SpawnDialog<ErrorNotificationDialog>();
            dialog.Initialize("There was an error saving the file. Please check the log file.");
            RightHeaderText.instance?.ShowError();
            return false;
        }
    }
    
    private static bool _InternalSaveFile(bool showSaved, string directory = null, string name = null)
    {
        name ??= Path.GetFileNameWithoutExtension(ChartPath);
        directory ??= FolderPath;
        
        PenguinWriter.WritePenguin(
            directory, 
            Metadata, 
            CompileAllInstruments(), 
            name
            );
        
        if (showSaved) RightHeaderText.instance?.ShowSaved();

        saved = true;
        return true;
    }

    private static bool InternalNewFile()
    {
        try
        {
            return _InternalNewFile();
        }
        catch (Exception e)
        {
            Debug.Log($"Error when creating new file.\n\t{e}");
            var dialog = DialogManager.SpawnDialog<ErrorNotificationDialog>();
            dialog.Initialize("There was an error creating new file. Please check the log file.");
            return false;
        }
    }
    
    private static bool _InternalNewFile()
    {
        var pathCandidate = StandaloneFileBrowser.SaveFilePanel("Open save location", "", ChartName, "penguin");
        
        if (string.IsNullOrEmpty(pathCandidate)) return false;
        
        ChartPath = pathCandidate;
        FolderPath = Path.GetDirectoryName(ChartPath);

        ChartLoading = true;
        
        Resolution = settings.DefaultResolution;
        
        ApplyFileInformation(new ChartFileInformation(
            new Metadata(), 
            new List<IInstrument>(), 
            new SyncTrackInstrument(), 
            new StarpowerInstrument(), 
            new SectionInstrument())
        );
        
        // also resets audio
        AudioManager.CreateAudioStreams();
        
        ChartLoading = false;
        ChartFileLoaded?.Invoke();
        
        SaveFile();
        
        // do this to avoid nasty errors with trying to load invalid data (which WILL happen if the active tab is not
        // reloaded). InPlaceRefresh() does not work here.
        SceneTabSwitcher.FullRefreshLoadedTab();
        return true;
    }

    private static bool InternalSaveFileAs()
    {
        try
        {
            return _InternalSaveFileAs();
        }
        catch (Exception e)
        {
            Debug.Log($"Error when saving file as. \n\t{e}");
            var dialog = DialogManager.SpawnDialog<ErrorNotificationDialog>();
            dialog.Initialize("There was an error saving the file to a new location. " +
                              "Please check the log file.");
            return false;
        }
    }
    
    private static bool _InternalSaveFileAs()
    {
        InternalSaveFile(false);
        var pathCandidate = StandaloneFileBrowser.SaveFilePanel("Open save location", "", "untitled", "penguin");
        
        if (string.IsNullOrEmpty(pathCandidate)) return false;

        ChartPath = pathCandidate;
        FolderPath = Path.GetDirectoryName(ChartPath);

        InternalSaveFile(true);
        return true;
    }

    public delegate void ChartFileLoadedDel();

    public static event ChartFileLoadedDel ChartFileLoaded;

    public static bool ChartLoading { get; private set; }

    private static bool InternalLoadFile()
    {
        try
        {
            return _InternalLoadFile();
        }
        catch (Exception e)
        {
            Debug.Log($"Error when loading file.\n\t{e}");
            ShowLoadError();
            return false;
        }
    }

    private static void ShowLoadError()
    {
        var dialog = DialogManager.SpawnDialog<ErrorNotificationDialog>();
        dialog.Initialize("There was an error loading the file. Please check the log file.");
    }

    private static bool _InternalLoadFile()
    {
        var pathCandidates = 
            StandaloneFileBrowser.OpenFilePanel
            (
                $"Open chart file", 
                "", 
                new[]
                {
                    new ExtensionFilter(
                        "Supported chart/save data formats", 
                        "chart", "penguin", "pce")
                }, 
                false
            );
        if (pathCandidates.Length < 1) return false;

        return _InternalLoadFile(pathCandidates[0]);
    }

    private static readonly string[] supportedFileFormats =
    {
        "opus",
        "ogg",
        "mp3",
        "flac",
        "wav"
    };
    
    private static bool _InternalLoadFile(string filePath)
    {
        openWithFileError = false;
        
        ChartPath = filePath;
        FolderPath = Path.GetDirectoryName(ChartPath);
        
        ChartLoading = true;

        var fileType = Path.GetExtension(ChartPath);
        ChartFileInformation readData;
        
        var startTime = Time.realtimeSinceStartup;
        
        // Diagnostics: file parsing is good chunk of load time for non-penguin files
        switch (fileType.ToLower())
        {
            case ".chart":
            {
                print($"Beginning new parsing operation on a .chart file with path {ChartPath}\n");
                readData = ChartParser.ParseChart(ChartPath);
                break;
            }
            case ".penguin" or ".pce" or ".pen": // .PEN happens when the file path is really long
            {
                print($"Beginning new parsing operation on a penguin save file with path {ChartPath}\n");
                readData = PenguinParser.ParsePenguin(ChartPath);
                break;
            }
            default:
            {
                throw new ArgumentException($"No support for parsing file type {fileType}.");
            }
        }

        print($"\nFile data successfully parsed. {(Time.realtimeSinceStartup - startTime)*1000}ms.");
        
        ApplyFileInformation(readData);
        
        if (Metadata.StemPaths.Count == 0)
        {
            // also need to parse chart stems
            // find properly named files - add to stems
            // find other audio files - ask to assign
            // testing: please add audio selection in future if excess audio files are found
            foreach (StemType key in Enum.GetValues(typeof(StemType)))
            {
                foreach (var format in supportedFileFormats)
                {
                    string targetFilePath = $"{FolderPath}/{key}.{format}";
                    if (File.Exists(targetFilePath))
                    {
                        Metadata.StemPaths[key] = targetFilePath;
                    }
                }
            } 
        }
        
        AudioManager.CreateAudioStreams();
        
        // Diagnostics: lots of load time here due to data that must be fetched
        Waveform.InitializeWaveformData();

        ChartLoading = false;
        
        ChartFileLoaded?.Invoke();
        
        // do this to avoid nasty errors with trying to load invalid data (which WILL happen if the active tab is not
        // reloaded). InPlaceRefresh() does not work here.
        SceneTabSwitcher.FullRefreshLoadedTab();
        
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
        
        fileLoaded = true;
    }

    public static void ExportFile(string targetDirectory)
    {
        try
        {
            _ExportFile(targetDirectory);
        }
        catch (Exception e)
        {
            Debug.Log($"Error when exporting file.\n\t{e}");
            var dialog = DialogManager.SpawnDialog<ErrorNotificationDialog>();
            dialog.Initialize("There was an error exporting the file. Please check the log file.");
        }
    }
    
    // Currently written for .chart exclusively, rework for other formats later
    private static void _ExportFile(string targetDirectory)
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

#if UNITY_STANDALONE_WIN
        // weird preceding thing is to fix errors resulting from long file paths? apparently?
        // it works so i am not touching it
        targetDirectory = Path.Combine(@"\\?\", targetDirectory); 
#endif

        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, true);
        }
        
        Directory.CreateDirectory(targetDirectory);
        
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
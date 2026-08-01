using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.InputSystem;
using Application = UnityEngine.Application;

public class UserSettings
{
    #region User editable

    public enum SettingProperty
    {
        minSustainLength,
        calibration,
        defaultResolution,
        scrollSensitivity,
        sustainGapTicks,
        maximumSavedUndoActions
    }
    
    public void SetChartingSetting(SettingProperty property, int input)
    {
        switch (property)
        {
            case SettingProperty.minSustainLength:
                MinimumSustainLengthSeconds = input / 1000.0f;
                break;
            case SettingProperty.calibration:
                Calibration = input;
                break;
            case SettingProperty.defaultResolution:
                DefaultResolution = input;
                break;
            case SettingProperty.scrollSensitivity:
                ScrollSensitivity = input;
                break;
            case SettingProperty.sustainGapTicks:
                SustainGapTicks = input;
                break;
            case SettingProperty.maximumSavedUndoActions:
                MaximumSavedUndoActions = input;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, null);
        }
    }
    
    public int GetChartingSetting(SettingProperty property)
    {
        return property switch
        {
            SettingProperty.minSustainLength => (int)(MinimumSustainLengthSeconds * 1000),
            SettingProperty.calibration => Calibration,
            SettingProperty.defaultResolution => DefaultResolution,
            SettingProperty.scrollSensitivity => ScrollSensitivity,
            SettingProperty.sustainGapTicks => SustainGapTicks,
            SettingProperty.maximumSavedUndoActions => MaximumSavedUndoActions,
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
    }
    
    public float MinimumSustainLengthSeconds = 0.2f;

    // Calibration is a lie. It is a lie told by the AudioManager to SongTime about where the song actually is.
    // In AudioManager, the internal positions of each of the audio streams are offset by {Calibration} seconds, and when
    // SongTime polls the audio position, AudioManager offsets it back the opposite direction. Effectively, the internal
    // audio position of the audio is shifted away the shown/true/expected position. On certain machines with high audio lag
    // (due to ancient tech or driver lag or whatever), there is a delay when playing the audio (audio plays significantly later than what is expected.
    // Obviously bad for rhythm games. This calibration "lie" allows the audio position to be offset by that delay so that
    // in effect there is no delay. The delay is usually so small it doesn't matter much, especially when tempo mapping
    // is de facto required anyway and done to the waveform, and charting is done to the tempo map. Older/slower machines
    // have a delay issue, which is what this fixes.
    // A negative offset will "push" the audio further into the chart, and a positive value will "pull" it back.
    public int Calibration = 0;

    /// <summary>
    /// Value autofilled into "Resolution" box upon new song creation.
    /// </summary>
    public int DefaultResolution = 192;
    public int ScrollSensitivity = 10;
    public int SustainGapTicks = 50;
    public int MaximumSavedUndoActions = 1024;
    
    #endregion
    
    #region User editable, not yet implemented
    
    public bool ShowSidebarSections = true;
    public bool LeftyFlip;
    public bool OpenNoteAsFret = false;
    public string MetadataImagePaths => $"{Chart.FolderPath}";
    
    /// ---
    /// Should these be settings/features?
    ///
    
    public float TimeToCullObjects = 5.0f;
    public float ButtonScrollSensitivity = 0.025f;
    
    // used only for .chart [Song] headers, no bearing on .ini files
    // in milliseconds!!
    public int DefaultPreviewLength = 3000;
    
    /// ---
    
    #endregion

    #region In-scene settings

    public bool ExtendedSustains = true; 
    public bool SoloPlacingAllowed = true;
    
    // Doesn't work properly as of current.
    public float userSetHighwayLength = 75.0f;

    #endregion
    
    #region Disk

    private static string SettingsDirectoryPath => Path.Combine(Application.persistentDataPath, "settings");
    private static string SettingsFilePath => Path.Combine(SettingsDirectoryPath, "settings.json");
    private static string CosmeticSettingsFilePath => Path.Combine(SettingsDirectoryPath, "cosmetics.json");
    private static string CustomKeybindsFilePath => Path.Combine(SettingsDirectoryPath, "inputs.json");
    private static string ExportSettingsFilePath => Path.Combine(SettingsDirectoryPath, "export.json");
    
    public void SaveSettingsToDisk()
    {
        if (!Directory.Exists(SettingsDirectoryPath))
        {
            Directory.CreateDirectory(SettingsDirectoryPath);
        }
        File.WriteAllText(SettingsFilePath, JsonUtility.ToJson(this));
        SaveCustomKeybinds();
        var cosmetics = new UniversalCosmeticSettings();
        cosmetics.WriteToDisk(CosmeticSettingsFilePath);
    }

    public static UserSettings ReadFromDisk()
    {
        LoadCustomKeybinds();
        
        if (File.Exists(SettingsFilePath))
        {
            return (UserSettings)JsonUtility.FromJson(File.ReadAllText(SettingsFilePath), typeof(UserSettings));
        }
        
        return new UserSettings();
    }

    public static void LoadCosmeticSettings()
    {
        UniversalCosmeticSettings.ApplySavedSettings(CosmeticSettingsFilePath);
    }

    public static void SaveExportSettings(ExportSettings settings)
    {
        File.WriteAllText(ExportSettingsFilePath, JsonUtility.ToJson(settings));
    }
    
    public static ExportSettings ReadExportSettingsFromDisk()
    {
        return (ExportSettings)JsonUtility.FromJson(File.ReadAllText(ExportSettingsFilePath), typeof(ExportSettings));
    }

    public static void SaveCustomKeybinds()
    {
        var keybindList = new CustomKeybindList();
        
        foreach (var action in KeybinderManager.GetEditableInputActions())
        {
            keybindList.actions.Add(new CustomKeybind(action));
        }

        var json = JsonUtility.ToJson(keybindList);
        File.WriteAllText(CustomKeybindsFilePath, json);
    }

    private static void ApplyKeybindFromLayout(InputAction action, KeybindLayout layout)
    {
        if (layout.paths.Count == 0) return;
        switch (layout.modifierType)
        {
            case KeybindLayout.ModifierType.none:
                action.AddBinding(layout.paths[0]);
                return;
            case KeybindLayout.ModifierType.OneModifier:
                action.AddCompositeBinding("OneModifier").
                    With("Modifier", layout.paths[0]).
                    With("Binding", layout.paths[1]);
                return;
            case KeybindLayout.ModifierType.TwoModifiers:
                action.AddCompositeBinding("TwoModifiers").
                    With("Modifier1", layout.paths[0]).
                    With("Modifier2", layout.paths[1]).
                    With("Binding", layout.paths[2]);
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void LoadCustomKeybinds()
    {
        if (!File.Exists(CustomKeybindsFilePath)) return;

        var json = (CustomKeybindList)JsonUtility.FromJson(File.ReadAllText(CustomKeybindsFilePath), typeof(CustomKeybindList));
        
        Chart.instance.inputMap.Disable();
        foreach (var action in KeybinderManager.GetEditableInputActions())
        {
            // Bindings shift back as you erase them so you have to just wait until it's empty.
            while (action.bindings.Count > 0)
            {
                action.ChangeBinding(0).Erase();
            }

            var jsonAction = json.actions.Find(x => x.actionGUID == action.id.ToString());
            
            if (jsonAction.action1 is not null) ApplyKeybindFromLayout(action, jsonAction.action1);
            if (jsonAction.action2 is not null) ApplyKeybindFromLayout(action, jsonAction.action2);
        }
    }
    
    #endregion
}

#region Keybind structs

/*
 * Unity's input system doesn't take too kindly to saving & loading keybinds through built-in methods for whatever reason
 * so I have implemented a basic saving/loading system for editable keybinds. Any built-in methods I found either didn't work
 * or saved keybinds but bricked Penguin after loading them. (FFS Unity, how do you screw it up this bad?? What really irks
 * me is that there seems to be an excess of documentation about this system online but a lack of USEFUL documentation for common
 * scenarios & edge cases. There is somehow swaths of documentation about but none of it actually says anything...)
 *
 * Custom solution is mainly designed for buttons, onemodifier, twomodifiers.
 * 
 * General structure: All keybinds are saved to a CustomKeybindList as CustomKeybind objects that store actions by
 * GUID:<actions> pairs. KeybindLayout stores the action data via input system key paths in a list.
 * 
 * JsonUtility saves & loads CustomKeybindList and UserSettings packs/unpacks the data into CustomKeybindList.
 * 
 * This was all hodgepodged together out of pure frustration and rage over Unity's terrible input system so the
 * architecture is kinda shit. It works though! Feel free to optimize if you want because it almost certainly needs
 * it.
 * - Emperor
 */

[System.Serializable]
public class CustomKeybindList
{
    public List<CustomKeybind> actions = new();
}

[System.Serializable]
public class CustomKeybind
{
    public string actionGUID;
    public KeybindLayout action1;
    public KeybindLayout action2;

    internal CustomKeybind(InputAction action)
    {
        actionGUID = action.id.ToString();
        var detectedKeybinds = DetectKeybinds(action);
        action1 = detectedKeybinds[0];
        action2 = detectedKeybinds[1];
    }
    
    public List<KeybindLayout> DetectKeybinds(InputAction action)
    {
        var kl = new List<KeybindLayout> { null, null };
        int klIndex = 0;
        
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var layout = new KeybindLayout();
            var identifier = action.bindings[i];

            switch (identifier.path)
            {
                case KeybindEditor.ONE_MODIFIER:
                    layout.modifierType = KeybindLayout.ModifierType.OneModifier;
                    layout.paths = new List<string>
                    {
                        action.bindings[i + 1].effectivePath, 
                        action.bindings[i + 2].effectivePath
                    };
                    
                    i += 2; // modifier + control
                    break;
                case KeybindEditor.TWO_MODIFIERS:
                    layout.modifierType = KeybindLayout.ModifierType.TwoModifiers;
                    layout.paths = new List<string>
                    {
                        action.bindings[i + 1].effectivePath,
                        action.bindings[i + 2].effectivePath, 
                        action.bindings[i + 3].effectivePath
                    };
                    
                    i += 3; // modifier + modifier + control
                    break;
                default:
                    layout.modifierType = KeybindLayout.ModifierType.none;
                    layout.paths = new List<string>
                    {
                        identifier.effectivePath
                    };
                    break;
            }
            
            if (klIndex < 2) kl[klIndex] = layout;
            else kl.Add(layout);
            klIndex++;
        }

        return kl;
    }
}

[System.Serializable]
public class KeybindLayout
{
    public enum ModifierType
    {
        none,
        OneModifier,
        TwoModifiers
    }

    public ModifierType modifierType;
    public List<string> paths;
}

#endregion

// Consider this a gathering mechanism - get everything in one place, then serialize it.
// Works well with unity objects via JsonUtility in case they are ever required (which is likely as cosmetic options
// increase in future). While I would like a cleaner functional solution I think this is the best way to keep this
// future-aware.
internal class UniversalCosmeticSettings
{
    // public so that JsonUtility can serialize
    public float hyperspeed = Waveform.ShrinkFactor;
    public float amplitude = Waveform.Amplitude;
    public float playSpeed = AudioManager.AudioSpeed;
    public float highwayLength = Highway.highwayLength;
    public double masterVolume = AudioManager.MasterVolume;
    public float metronomeVolume = AudioManager.GetSFXVolume(AudioManager.SFX.metronome);
    public float clapVolume = AudioManager.GetSFXVolume(AudioManager.SFX.clap);

    public void WriteToDisk(string filePath)
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
        }
        
        File.WriteAllText(filePath, JsonUtility.ToJson(this));
    }

    public static void ApplySavedSettings(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var savedData =
            (UniversalCosmeticSettings)JsonUtility.FromJson(
                File.ReadAllText(filePath),
                typeof(UniversalCosmeticSettings)
            );

        Waveform.ShrinkFactor = savedData.hyperspeed;
        Waveform.Amplitude = savedData.amplitude;
        AudioManager.AudioSpeed = savedData.playSpeed;
        Highway.highwayLength = savedData.highwayLength;
        AudioManager.MasterVolume = savedData.masterVolume;
        AudioManager.SetSFXVolume(AudioManager.SFX.metronome, savedData.metronomeVolume);
        AudioManager.SetSFXVolume(AudioManager.SFX.clap, savedData.clapVolume);
    }
} 

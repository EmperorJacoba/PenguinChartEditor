using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB; // system file browser

public class ChartMetadata : MonoBehaviour
{
    /// <summary>
    /// Stores valid song metadata fields.
    /// </summary>
    public enum MetadataType
    {
        name,
        artist,
        album,
        genre,
        year,
        charter,
        song_length,
        preview_start_time,
        icon,
        loading_phrase,
        album_track,
        playlist_track,
        video_start_time,
    }

    public Dictionary<MetadataType, string> songInfo = new();

    // All of these values store difficulties in a value from 0-6, although values higher than 6 are allowed for some niche CH uses.
    // Set up these values in the CHART tab - no sense setting them up when you don't have the tracks charted yet!
    /// <summary>
    /// Stores valid instrument difficulties.
    /// </summary>
    public enum InstrumentDifficultyType
    {
        diff_band,
        diff_guitar,
        diff_guitar_coop,
        diff_rhythm,
        diff_bass,
        diff_drums,
        diff_drums_real,
        diff_elite_drums,
        diff_keys,
        diff_keys_real,
        diff_ghl,
        diff_bass_ghl,
        diff_vocals,
        diff_vocals_harm
    }

    public Dictionary<InstrumentDifficultyType, int> difficulties = new();

    /// <summary>
    /// Stores valid types of audio stems.
    /// </summary>
    public enum StemType
    {
        song,
        guitar,
        bass,
        rhythm,
        keys,
        vocals,
        vocals_1,
        vocals_2,
        drums,
        drums_1,
        drums_2,
        drums_3,
        drums_4
    }

    public Dictionary<StemType, string> stems = new();

    private int chartResolution;
    /// <summary>
    /// Number of ticks per quarter note (VERY IMPORTANT FOR SONG RENDERING)
    /// </summary>
    public string ChartResolution 
    {
        set 
        {
            if (!int.TryParse(value, out int tempResolution))
            {
                throw new ArgumentException("Resolution must be an integer!");
            }

            if (tempResolution < 192)
            {
                throw new ArgumentException("Chart resolution must be greater than 192 ticks per quarter note!");
            }
            else
            {
                chartResolution = tempResolution;
            }
        }
    }

    /// <summary>
    /// Stores the directory of the album cover selected by the user.
    /// </summary>
    public string ImagePath { get; private set; } // set with SetAlbumCover()

    /// <summary>
    /// Stores the directory of the background set by the user.
    /// </summary>
    public string Background { get; private set;}

    [SerializeField] private TMP_InputField resolutionTextBox;
    // Use to setup DDOL & Data persistence
    public static ChartMetadata metadata;
    private void Awake()
    {
        if (metadata) return;

        metadata = this;
        DontDestroyOnLoad(gameObject);

        ChartResolution = Convert.ToString(UserSettings.DefaultResolution);
        resolutionTextBox.text = Convert.ToString(chartResolution);
    }

    /// <summary>
    /// Holds the current metadata type to edit in SetSongInfo().
    /// </summary>
    public string CurrentInputField {private get; set;}
    // No idea why but I can't pass in two values from an input field to the same function?? so i have to do this

    /// <summary>
    /// Initializes/edits the string value associated with each metadata key in songInfo
    /// </summary>
    /// <param name="userInput"></param>
    public void SetSongInfo(string userInput)
    {
        ClearSongInfo(CurrentInputField); // Empty the dictionary val first to avoid throwing error

        // Convert the string passed in from the InputField to the enum type MetadataType
        MetadataType selectedMetadataAsEnum = (MetadataType)Enum.Parse(typeof(MetadataType), CurrentInputField); 
        // Add the selected audio path to dictionary with key as the enum type of the string
        songInfo.Add(selectedMetadataAsEnum, userInput);
    }

    public void ClearSongInfo(string metadata)
    {
        MetadataType selectedMetadataAsEnum = (MetadataType)Enum.Parse(typeof(MetadataType), metadata); 
        if (songInfo.ContainsKey(selectedMetadataAsEnum))
        {
            songInfo.Remove(selectedMetadataAsEnum);
        }
    }



    // Set up extension filters for selecting files
    private readonly ExtensionFilter[] imageExtensions = new [] 
    {
        new ExtensionFilter("Image Files ", "png", "jpg", "jpeg"),
    };

    private readonly ExtensionFilter[] audioExtensions = new []
    {
        new ExtensionFilter("Audio Files ", "opus", "ogg", "mp3", "wav", "flac"),
    };

    // Stores button to set image of (one used to select image)
    [SerializeField] private Button imageSelector;

    /// <summary>
    /// Used on album button select click to set the image of the button based on user selection.
    /// </summary>
    public void UserSetAlbumCover()
    {
        var selectedImagePath = StandaloneFileBrowser.OpenFilePanel("Open album cover", "", imageExtensions, false); // User open image file dialog

        if (selectedImagePath.Length != 0) // Avoid throwing error when user cancels selection
        {
            ImagePath = selectedImagePath[0]; // Store path for exporting chart package later

            Texture2D albumCoverTexture = new(1, 1); // Set up texture, l&w args are irrelevant
            byte[] coverInBytes = File.ReadAllBytes(ImagePath); // Convert user selection to bytes to create new Texture

            if (!albumCoverTexture.LoadImage(coverInBytes)) // Load image into albumCoverTexture, if it no work then throw error
            {
                throw new ArgumentException("Image failed to load");
            }

            if (albumCoverTexture.height != 512 || albumCoverTexture.width != 512) // Future: Automatically resize image for user
            {
                throw new ArgumentException("Image must be 512x512 pixels! Use a program like Photoshop or GIMP to resize the image.");
            }

            Sprite albumCoverSprite = Sprite.Create(
                albumCoverTexture,
                new Rect(0, 0, 512, 512),
                new Vector2(0.5f, 0.5f)
            ); // Create sprite from user image
            imageSelector.GetComponent<Image>().sprite = albumCoverSprite; // Set image component of button to created sprite

            GameObject tempSelectionText = imageSelector.gameObject.transform.GetChild(0).gameObject;
            tempSelectionText.GetComponent<TextMeshProUGUI>().text = ""; // Get text child of button and set the text to empty upon image selection
        }
    }

    /// <summary>
    /// Set the audio stem of one of the eligible audio stems in StemType.
    /// </summary>
    /// <param name="selectedStem"></param>
    public void SetAudioStem(string selectedStem)
    {
        // This will be an audio file -> audioExtensions prevents user from selecting anything but an audio file
        var selectedAudioPath = StandaloneFileBrowser.OpenFilePanel($"Open {selectedStem} audio file", "", audioExtensions, false); // User open audio dialog

        if (selectedAudioPath.Length != 0) // Avoid throwing error when user cancels selection
        {
            ClearAudioStem(selectedStem); // Empty the dictionary val first to avoid throwing error

            // Convert the string passed in from the button to the enum type StemType
            StemType selectedStemAsEnum = (StemType)Enum.Parse(typeof(StemType), selectedStem); 
            // Add the selected audio path to dictionary with key as the enum type of the string
            stems.Add(selectedStemAsEnum, selectedAudioPath[0]);

            GetInputField(selectedStem).text = selectedAudioPath[0]; // Change the input field preview to the path of the audio file
        }
    }

    /// <summary>
    /// Gets the Input Field in a grouped Stem_Edit GameObject.
    /// </summary>
    /// <param name="selectedStem"></param>
    /// <param name="PathPreviewBoxIndex"></param>
    /// <returns>Path_PreviewBox TMP_InputField component.</returns>
    private TMP_InputField GetInputField(string selectedStem, int PathPreviewBoxIndex = 1)
    {
        // Find stem's parent GameObject containing the modifiers
        GameObject inputFieldParent = GameObject.Find($"{selectedStem}_Stem_Edit"); 
        // Get the Path_PreviewBox (change the var if you ever add more things to the parent)
        GameObject inputField = inputFieldParent.transform.GetChild(PathPreviewBoxIndex).gameObject;
        // Get the InputField component attached to the inputField GameObject
        return inputField.GetComponent<TMP_InputField>();
    }

    /// <summary>
    /// Remove the audio stem associated with a key from StemType in stems dictionary.
    /// </summary>
    /// <param name="selectedStem"></param>
    public void ClearAudioStem(string selectedStem)
    {
        StemType selectedStemAsEnum = (StemType)Enum.Parse(typeof(StemType), selectedStem);
        if (stems.ContainsKey(selectedStemAsEnum))
        {
            stems.Remove(selectedStemAsEnum);
            GetInputField(selectedStem).text = "";
        }
    }
}


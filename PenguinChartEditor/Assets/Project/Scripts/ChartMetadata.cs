using System;
using UnityEngine;
using UnityEngine.UI;
using SFB; // system file browser
using System.IO;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ChartMetadata : MonoBehaviour
{
    /// <summary>
    /// Name of the loaded song.
    /// </summary>
    public string Song_name { get; set; }

    /// <summary>
    /// Name of the loaded song artist.
    /// </summary>
    public string Artist { get; set; }

    /// <summary>
    /// Name of the loaded song's album.
    /// </summary>
    public string Album { get; set; }

    /// <summary>
    /// Genre of the loaded song
    /// </summary>
    public string Genre { get; set; }

    /// <summary>
    /// Release year of the loaded song. 
    /// </summary>
    public string Year { get; set; }

    /// <summary>
    /// Name of the charter(s) who have worked on the loaded song.
    /// </summary>
    public string Charter { get; set; }
    
    /// <summary>
    /// Length of the song in milliseconds.
    /// </summary>
    public int Song_length { get; private set; } // set through CalculateSongLength() in audio parser?
                                    // Note: set to the length of the longest audio stem


    // All of these values store difficulties in a value from 0-6, although values higher than 6 are allowed for some niche CH uses.
    // Set up these values in the CHART tab - no sense setting them up when you don't have the tracks charted yet!
    public int Diff_band { get; set; }
    public int Diff_guitar { get; set; }
    public int Diff_guitar_coop { get; set; }
    public int Diff_rhythm { get; set; }
    public int Diff_bass { get; set; }
    public int Diff_drums { get; set; }
    public int Diff_drums_real { get; set; }
    public int Diff_elite_drums { get; set; } // named as such to conform with requested diff format in elite drums specifications
    public int Diff_keys { get; set; }
    public int Diff_keys_real { get; set; } // for planned Pro Keys
    public int Diff_ghl { get; set; }
    public int Diff_bass_ghl { get; set; }
    public int Diff_vocals { get; set; } // for planned Vocals
    public int Diff_vocals_harm { get; set; }

    /// <summary>
    /// Time to start in-game song preview in milliseconds.
    /// </summary>
    public int Preview_start_time { get; private set;} // set through CalculatePreviewTime() -> take HH:MM:SS input from user, translate into ms
    
    /// <summary>
    /// Charter Icon ID or Source ID as listed in the Clone Hero Icons and Sources gitlab repo.
    /// <para> Link: https://gitlab.com/clonehero/sources </para>
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// Text shown in the "loading" screen (instrument/difficulty selection screen).
    /// </summary>
    public string Loading_phrase { get; set; }

    /// <summary>
    /// Position of the song within the album's track ordering
    /// </summary>
    public string Album_track { get; set; } // String to work with text input

    /// <summary>
    /// Position of the song in a setlist .
    /// </summary>
    public string Playlist_track { get; set; } // String to work with text input

    /// <summary>
    /// Is the chart a modchart?
    /// </summary>
    // public bool Modchart { get; set; } Not going to worry about this after all

    /// <summary>
    /// The time at which the video begins playing in the track, in milliseconds.
    /// </summary>
    public int Video_start_time { get; set; }

    /// <summary>
    /// Number of ticks per quarter note (VERY IMPORTANT FOR SONG RENDERING)
    /// </summary>
    public int Resolution { get; set; }

    /// <summary>
    /// Stores the directory of the album cover selected by the user.
    /// </summary>
    public string ImagePath { get; private set; } // set with SetAlbumCover()

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

    // Use to setup DDOL & Data persistence
    public static ChartMetadata metadata;
    private void Awake()
    {
        metadata = this;
        DontDestroyOnLoad(gameObject);
    }

    // Set up extension filters for selecting files
    private ExtensionFilter[] imageExtensions = new [] {
        new ExtensionFilter("Image Files ", "png", "jpg", "jpeg"),
        new ExtensionFilter("Other ", "*")
    };

    private ExtensionFilter[] audioExtensions = new []
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

    private TMP_InputField GetInputField(string selectedStem, int PathPreviewBoxIndex = 1)
    {
        // Find stem's parent GameObject containing the modifiers
        GameObject inputFieldParent = GameObject.Find($"{selectedStem}_Stem_Edit"); 
        // Get the Path_PreviewBox (change the var if you ever add more things to the parent)
        GameObject inputField = inputFieldParent.transform.GetChild(PathPreviewBoxIndex).gameObject;
        // Get the InputField component attached to the inputField GameObject
        return inputField.GetComponent<TMP_InputField>();
    }

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


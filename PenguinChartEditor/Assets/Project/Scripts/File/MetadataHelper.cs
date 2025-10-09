using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;
using System;
using System.IO; // system file browser

public class MetadataHelper : MonoBehaviour
{
    /// <summary>
    /// Holds the current metadata type to edit in SetSongInfo().
    /// </summary>
    public string CurrentInputField { private get; set; }
    // No idea why but I can't pass in two values from an input field to the same function?? so i have to do this

    [SerializeField] private TMP_InputField resolutionTextBox;
    // Stores button to set image of (one used to select image)
    [SerializeField] private Button imageSelector;

    void Awake()
    {
        resolutionTextBox.text = Convert.ToString(Chart.Resolution);
    }

    /// <summary>
    /// Initializes/edits the string value associated with each metadata key in songInfo
    /// </summary>
    /// <param name="userInput"></param>
    public void SetSongInfo(string userInput)
    {
        ClearSongInfo(CurrentInputField); // Empty the dictionary val first to avoid throwing error

        // Convert the string passed in from the InputField to the enum type MetadataType
        Metadata.MetadataType selectedMetadataAsEnum = (Metadata.MetadataType)Enum.Parse(typeof(Metadata.MetadataType), CurrentInputField);
        // Add the selected audio path to dictionary with key as the enum type of the string
        Chart.Metadata.SongInfo.Add(selectedMetadataAsEnum, userInput);
    }

    public void ClearSongInfo(string metadata)
    {
        Metadata.MetadataType selectedMetadataAsEnum = (Metadata.MetadataType)Enum.Parse(typeof(Metadata.MetadataType), metadata);
        if (Chart.Metadata.SongInfo.ContainsKey(selectedMetadataAsEnum))
        {
            Chart.Metadata.SongInfo.Remove(selectedMetadataAsEnum);
        }
    }

    // Set up extension filters for selecting files
    private readonly ExtensionFilter[] imageExtensions = new[]
    {
        new ExtensionFilter("Image Files ", "png", "jpg", "jpeg"),
    };

    private readonly ExtensionFilter[] audioExtensions = new[]
    {
        new ExtensionFilter("Audio Files ", "opus", "ogg", "mp3", "wav", "flac"),
    };

    /// <summary>
    /// Used on album button select click to set the image of the button based on user selection.
    /// </summary>
    public void UserSetAlbumCover()
    {
        var selectedImagePath = StandaloneFileBrowser.OpenFilePanel("Open album cover", "", imageExtensions, false); // User open image file dialog

        if (selectedImagePath.Length != 0) // Avoid throwing error when user cancels selection
        {
            Chart.Metadata.ImagePath = selectedImagePath[0]; // Store path for exporting chart package later

            Texture2D albumCoverTexture = new(1, 1); // Set up texture, l&w args are irrelevant
            byte[] coverInBytes = File.ReadAllBytes(Chart.Metadata.ImagePath); // Convert user selection to bytes to create new Texture

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
    /// Set the audio stem of one of the eligible audio stems in Metadata.StemType.
    /// </summary>
    /// <param name="selectedStem"></param>
    public void SetAudioStem(string selectedStem)
    {
        // This will be an audio file -> audioExtensions prevents user from selecting anything but an audio file
        var selectedAudioPath = StandaloneFileBrowser.OpenFilePanel($"Open {selectedStem} audio file", "", audioExtensions, false); // User open audio dialog

        if (selectedAudioPath.Length != 0) // Avoid throwing error when user cancels selection
        {
            ClearAudioStem(selectedStem); // Empty the dictionary val first to avoid throwing error

            // Convert the string passed in from the button to the enum type Metadata.StemType
            Metadata.StemType selectedStemAsEnum = (Metadata.StemType)Enum.Parse(typeof(Metadata.StemType), selectedStem);
            // Add the selected audio path to dictionary with key as the enum type of the string
            Chart.Metadata.StemPaths.Add(selectedStemAsEnum, selectedAudioPath[0]);

            GetInputField(selectedStem).text = selectedAudioPath[0]; // Change the input field preview to the path of the audio file
        }
    }

    /// <summary>
    /// Remove the audio stem associated with a key from Metadata.StemType in stems dictionary.
    /// </summary>
    /// <param name="selectedStem"></param>
    public void ClearAudioStem(string selectedStem)
    {
        Metadata.StemType selectedStemAsEnum = (Metadata.StemType)Enum.Parse(typeof(Metadata.StemType), selectedStem);
        if (Chart.Metadata.StemPaths.ContainsKey(selectedStemAsEnum))
        {
            Chart.Metadata.StemPaths.Remove(selectedStemAsEnum);
            GetInputField(selectedStem).text = "";
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

    public void SetResolution(string newRes)
    {
        Chart.Resolution = int.Parse(newRes);
    }
}
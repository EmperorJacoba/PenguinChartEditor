using System;
using UnityEngine;

public class SongTimelineManager : MonoBehaviour
{
    static InputMap inputMap;
    WaveformManager waveformManager;

    // Needed for delta calculations when scrolling using MMB
    private float initialMouseY = float.NaN;
    private float currentMouseY;

    public static double SongPosition
    {
        get
        {
            return _songPos;
        }
        private set
        {
            if (_songPos == value) return;
            value = Math.Round(value, 3); // So that CurrentWFDataPosition comes out clean
            _songPos = value;

            TimeChanged?.Invoke();
        }
    }
    private static double _songPos = 0; 

    public delegate void TimeChangedDelegate();
    public static event TimeChangedDelegate TimeChanged;

    void Awake()
    {
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.ScrollTrack.performed += scrollChange => ChangeTime(scrollChange.ReadValue<float>());

        inputMap.Charting.MiddleScrollMousePos.performed += x => currentMouseY = x.ReadValue<Vector2>().y;
        inputMap.Charting.MiddleScrollMousePos.Disable(); // This is disabled immediately so that it's not running when it's not needed

        inputMap.Charting.MiddleMouseClick.started += x => ChangeMiddleClick(true);
        inputMap.Charting.MiddleMouseClick.canceled += x => ChangeMiddleClick(false);
    }

    void Update()
    {
        if (inputMap.Charting.MiddleScrollMousePos.enabled)
        {
            ChangeTime(currentMouseY - initialMouseY, true);
            // This runs every frame to get that smooth scrolling effect like on webpages and such
            // If this ran when the mouse was moved then it would be super jumpy
        }

        if (PluginBassManager.AudioPlaying)
        {
            SongPosition = PluginBassManager.GetCurrentAudioPosition();
        }
    }

    public static void ToggleChartingInputMap()
    {
        if (inputMap.Charting.enabled) inputMap.Charting.Disable();
        else inputMap.Charting.Enable();
    }

    /// <summary>
    /// Enable/disable middle click movement upon press/release
    /// </summary>
    /// <param name="clickStatus">true = MMB pressed, false = MMB released</param>
    void ChangeMiddleClick(bool clickStatus)
    {
        if (clickStatus)
        {
            inputMap.Charting.MiddleScrollMousePos.Enable(); // Allow calculations of middle mouse scroll now that MMB is clicked

            initialMouseY = Input.mousePosition.y;
            currentMouseY = Input.mousePosition.y;
            // ^^ These HAVE to be here. I really didn't want to use the old input system for this (for unity in the literal sense)
            // but initial & current mouse positions need to be initialized right this instant in order to get a 
            // proper delta calculation in Update().
            // Without this the waveform jumps to the beginning when you click MMB without moving
        }
        else
        {
            inputMap.Charting.MiddleScrollMousePos.Disable();
            initialMouseY = float.NaN; 
            // Kind of a relic from testing, but I'm keeping this here because I feel like this is somewhat helpful in case this is improperly used somewhere
        }
    }

    void ChangeTime(float scrollChange, bool middleClick = false)
    {
        if (float.IsNaN(scrollChange)) return; // for some reason when the input map is reenabled it passes NaN into this function so we will be having none of that thank you 

        var scrollSuppressant = 1;
        if (middleClick) scrollSuppressant = 50;
        SongPosition += scrollChange / (UserSettings.Sensitivity * scrollSuppressant);

        if (SongPosition < 0)
        {
            SongPosition = 0;
        }
        else if (SongPosition > PluginBassManager.SongLength)
        {
            SongPosition = PluginBassManager.SongLength;
        }
    }
}

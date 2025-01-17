using UnityEngine;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Opus;
using System;
using System.Linq;
using UnityEditor.PackageManager.UI;
using Unity.Collections;

public class PluginBassManager : MonoBehaviour
{
    int globalStream;
    int globalStreamNotBuffered;
    int sampleRate = 44100;

    public int width = 1024;
    public int height = 64;

    [SerializeField] Color waveformBackground = Color.black;
    [SerializeField] Color waveformDrawnColor = Color.yellow;
    private void Awake() 
    {
        InitializeAudio();
        GetWaveform();
    }

    // This code is adapted from https://github.com/omarvision/waveform-2D/
    // I will forever be in your debt, Omar, thank you.
    public Texture2D GetWaveform() // This currently outputs a texture and WORKS!! But textures are limited to 16384 pixel width, so new approach is needed
    // MS method of connecting points is probably the way to go
    // Tried and tested, if it ain't broke don't fit it
    // this attempt is an illustration of that
    // BUT AT LEAST I GOT THE WAVEFORM ARRAY & RESOLUTION STUFF FIGURED OUT YIPPEE
    // still unoptimized but whatever
    // live and learn baby
    {
        var halfheight = height / 2; 

        Texture2D drawnWaveform = new(width, height, TextureFormat.RGBA32, false); // Holds Texture for waveform

        Color32[] WfBg = new Color32[width*height];
        for (int i = 0; i < WfBg.Length; i++)
        {
            WfBg[i] = waveformBackground;
        }
        drawnWaveform.SetPixels32(WfBg);

        var waveformData = GetAudioSamples(); // Load in waveform data 

        var pixelMFactor = height / 1.5;

        for (var i = 0; i < waveformData.Length; i++)
        {
            waveformData[i] = (float)Math.Floor(waveformData[i] * pixelMFactor);
        }

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < waveformData[x]; y++)
            {
                drawnWaveform.SetPixel(x, halfheight + y, waveformDrawnColor);
                drawnWaveform.SetPixel(x, halfheight - y, waveformDrawnColor);
            }
        }

        drawnWaveform.filterMode = FilterMode.Point;
        drawnWaveform.Apply(false);
        return drawnWaveform;
    }

    private float[] GetAudioSamples()
    {
        globalStreamNotBuffered = Bass.BASS_StreamCreateFile(
            "G:/_PCE_files/TestAudioFiles/songtest.mp3", 
            0, 0, 
            BASSFlag.BASS_SAMPLE_FLOAT | 
            BASSFlag.BASS_STREAM_DECODE | 
            BASSFlag.BASS_STREAM_PRESCAN
        ); // Loads in file stream to get waveform data from

        var songLengthBytes = Bass.BASS_ChannelGetLength(globalStreamNotBuffered); // Get # of bytes in song
        var floatArrayLength = songLengthBytes/4; // # of vals in float[] array will be 1/4 of this bc reasons

        float[] allSamples = new float[floatArrayLength]; // Get an array of every single sample (inefficient, but works right now)
        var x = Bass.BASS_ChannelGetData(globalStreamNotBuffered, allSamples, (int)songLengthBytes); // TAKE FEWER SAMPLES TO SPEED UP PROCESS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        float[] waveformData = new float[width]; // Array of vals that holds one point for every pixel of end-goal image to be generated
        // In future: Width needs to scale on track length so that track can be navigable in MS like interface

        int packsize = (int)floatArrayLength / width; // Number of samples to skip between each grab

        for (int w = 0; w < width; w++)
        {
            waveformData[w] = Mathf.Abs(allSamples[w * packsize]); // Each pos in simplified array is set to a value from all of the samples based on the skip interval 
        }
        return waveformData;
        // This array has values ranging from 0 to 1
        // Figure out how to scale that up for a Texture properly
        // Edit: texture no workie
        // Most of this code will still work (minus the for loop, which is specifically formatted for pixels)
        // I could swap width in pixels for a resolution sort of thing but then what's even the point
        // Use BASS handbook example to generate this data
        // Then connect points like MS
    }

    void InitializeAudio()
    {
        if (Bass.BASS_Init(-1, sampleRate, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
        {
            Bass.BASS_PluginLoadDirectory($"{Application.dataPath}/Plugins/Bassx64");

            globalStream = Bass.BASS_StreamCreateFile("G:/_PCE_files/TestAudioFiles/songtest.mp3", 0, 0, BASSFlag.BASS_DEFAULT);
            if (globalStream != 0)
            {
                Bass.BASS_ChannelPlay(globalStream, false);
            }
        }
        else
        {
            Debug.Log($"Init failed");
        }
    }

    void OnApplicationQuit()
    {
        Bass.BASS_Free();     
    }
}

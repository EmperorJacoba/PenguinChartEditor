using UnityEngine;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Opus;
using System;
using UnityEngine.UIElements;

public class PluginBassManager : MonoBehaviour
{
    int globalStream;
    int globalStreamNotBuffered;
    int sampleRate = 44100;

    public int testWidth = 1024;
    public int testHeight = 64;

    [SerializeField] Color waveformBackground = Color.black;
    [SerializeField] Color waveformDrawnColor = Color.yellow;
    private void Awake() 
    {
        InitializeAudio();
        GetWaveform();
    }

    // This code is adapted from https://github.com/omarvision/waveform-2D/
    // I will forever be in your debt, Omar, thank you.
    public Texture2D GetWaveform()
    {
        var halfheight = testHeight / 2; 
        float heightScale = (float)testWidth * 0.9f;

        Texture2D drawnWaveform = new(testWidth, testHeight, TextureFormat.RGBA32, false); // Holds Texture for waveform
        var waveformData = GetAudioSamples(); // Load in waveform data 

        for (int x = 0; x < testWidth; x++)
        {
            for (int y = 0; y < testHeight; y++)
            {
                drawnWaveform.SetPixel(x, y, waveformBackground); // Blank out background (why is it so transparent?? fix that?? should be opaque??)
            }
        }

        for (int x = 0; x < testWidth; x++)
        {
            for (int y = 0; y < waveformData[x] * heightScale; y++)
            {
                drawnWaveform.SetPixel(x, halfheight + y, waveformDrawnColor); // This method of calculating pixel length does not work with the way BASS outputs sample data
                drawnWaveform.SetPixel(x, halfheight - y, waveformDrawnColor); // Figure out new way
            }
        }
        return drawnWaveform;
    }

    private float[] GetAudioSamples()
    {
        var songLengthBytes = Bass.BASS_ChannelGetLength(globalStreamNotBuffered); // Get # of bytes in song
        var floatArrayLength = songLengthBytes/4; // # of vals in float[] array will be 1/4 of this bc reasons

        float[] allSamples = new float[floatArrayLength]; // Get an array of every single sample (inefficient, but works right now)
        var x = Bass.BASS_ChannelGetData(globalStreamNotBuffered, allSamples, (int)songLengthBytes); // TAKE FEWER SAMPLES TO SPEED UP PROCESS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        float[] waveformData = new float[testWidth]; // Array of vals that holds one point for every pixel of end-goal image to be generated
        // In future: Width needs to scale on track length so that track can be navigable in MS like interface

        int packsize = (int)floatArrayLength / testWidth; // Number of samples to skip between each grab

        for (int w = 0; w < testWidth; w++)
        {
            waveformData[w] = Mathf.Abs(allSamples[w * packsize]); // Each pos in simplified array is set to a value from all of the samples based on the skip interval 
        }
        foreach (var datum in waveformData)
        {
            Debug.Log($"{datum}"); // Log data to console to show it works
        }
        return waveformData;
        // This array has values ranging from 0 to 1
        // Figure out how to scale that up for a Texture properly
    }

    void InitializeAudio()
    {
        if (Bass.BASS_Init(-1, sampleRate, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
        {
            globalStream = Bass.BASS_StreamCreateFile("G:/_PCE_files/TestAudioFiles/songtest.mp3", 0, 0, BASSFlag.BASS_DEFAULT);
            globalStreamNotBuffered = Bass.BASS_StreamCreateFile("G:/_PCE_files/TestAudioFiles/songtest.mp3", 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_STREAM_PRESCAN);

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

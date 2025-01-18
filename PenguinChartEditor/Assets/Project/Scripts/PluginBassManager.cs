using UnityEngine;
using Un4seen.Bass;
using System;

public class PluginBassManager : MonoBehaviour
{
    int globalStream;
    int trackStream;
    int sampleRate = 44100;

    public int width = 1024;
    public int height = 64;

    [SerializeField] Color waveformBackground = Color.black;
    [SerializeField] Color waveformDrawnColor = Color.yellow;
    private void Awake() 
    {
        InitializeAudio();
        GetAudioSamples();
    }

    /// <summary>
    /// Returns a simplified float array of an audio file's sample data.
    /// </summary>
    /// <param name="songPath"></param>
    /// <returns>waveformData (float[]) that contains selected audio samples.</returns>
    private float[] GetAudioSamples(string songPath = "G:/_PCE_files/TestAudioFiles/songtest.mp3") // testing path as default value
    {
        trackStream = Bass.BASS_StreamCreateFile(
            songPath, 
            0, 0, 
            BASSFlag.BASS_SAMPLE_FLOAT | 
            BASSFlag.BASS_STREAM_DECODE | 
            BASSFlag.BASS_STREAM_PRESCAN
        ); // Loads in file stream to get waveform data from

        var songLengthBytes = Bass.BASS_ChannelGetLength(trackStream); // Get # of bytes in song
        var floatArrayLength = songLengthBytes/4; // # of vals in float[] array will be 1/4 of this bc 4 bytes per 32 bit float

        float[] allSamples = new float[floatArrayLength];
        var x = Bass.BASS_ChannelGetData(trackStream, allSamples, (int)songLengthBytes); // Get an array of every single sample

        var sampleIntervalBytes = Bass.BASS_ChannelSeconds2Bytes(trackStream, 0.001) / 4; // Number of bytes in 1 ms of audio
        var compArraySize = (int)Math.Floor((double)songLengthBytes / sampleIntervalBytes) / 4; // Number of samples to compress down to for line rendering
        float[] waveformData = new float[compArraySize]; // Array of vals to hold compressed data
        for (var i = 0; i < compArraySize; i++)
        {
            waveformData[i] = Math.Abs(allSamples[i * sampleIntervalBytes]); // Select a sample every 1 ms and store it in the compressed array
        }

        return waveformData;
    }

    void InitializeAudio()
    {
        if (Bass.BASS_Init(-1, sampleRate, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
        {
            Bass.BASS_PluginLoadDirectory($"{Application.dataPath}/Plugins/Bassx64");

            globalStream = Bass.BASS_StreamCreateFile("G:/_PCE_files/TestAudioFiles/songtest.mp3", 0, 0, BASSFlag.BASS_DEFAULT);
            if (globalStream != 0)
            {
                //Bass.BASS_ChannelPlay(globalStream, false);
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

using UnityEngine;
using Un4seen.Bass;
using System;

public class PluginBassManager : MonoBehaviour
{
    int globalStream;
    int sampleRate = 44100;

    private void Awake() 
    {
        InitializeAudio();
        //GetAudioSamples();
    }

    /// <summary>
    /// Returns a simplified float array of an audio file's sample data.
    /// </summary>
    /// <param name="songPath"></param>
    /// <returns>waveformData (float[]) that contains selected audio samples.</returns>
    public float[] GetAudioSamples(string songPath = "G:/_PCE_files/TestAudioFiles/songtest.mp3") // testing path as default value
    {
        var currentTrackStream = Bass.BASS_StreamCreateFile(
            songPath, 
            0, 0, 
            BASSFlag.BASS_SAMPLE_FLOAT | 
            BASSFlag.BASS_STREAM_DECODE | 
            BASSFlag.BASS_STREAM_PRESCAN
        ); // Loads in file stream to get waveform data from

        
        var songLengthBytes = Bass.BASS_ChannelGetLength(currentTrackStream); // Get # of bytes in song
        var floatArrayLength = songLengthBytes/4; // # of vals in float[] array will be 1/4 of this bc 4 bytes per 32 bit float

        float[] allSamples = new float[floatArrayLength];
        var x = Bass.BASS_ChannelGetData(currentTrackStream, allSamples, (int)songLengthBytes); // Get an array of every single sample
        // This is faster than taking individual samples using setPosition and then ChannelGetData

        allSamples = ConvertStereoSamplestoMono(allSamples); // Convert stereo samples to mono

        var compressedArrayResolution = 0.001f; // Holds a value in seconds for how often to take a sample from all samples 
                                                // needs to be reeeeeally small for a good waveform - currently too big
                                                // Maybe just do it directly in bytes later on?
        var sampleIntervalBytes = Bass.BASS_ChannelSeconds2Bytes(currentTrackStream, compressedArrayResolution) / 8; // Number of bytes in x seconds of audio
        var compArraySize = (int)Math.Floor((double)songLengthBytes / sampleIntervalBytes) / 8; // Number of samples to compress down to for line rendering

        float[] waveformData = new float[compArraySize]; // Array of vals to hold compressed data
        for (var i = 0; i < compArraySize; i++)
        {
            waveformData[i] = Math.Abs(allSamples[i * sampleIntervalBytes]); // Select abs val of sample every 1 ms from all the samples and store it in the compressed array
        }

        return waveformData;
        
    }

    /// <summary>
    /// Converts stereo samples to mono samples to get a more accurate waveform.
    /// </summary>
    /// <param name="samples"></param>
    /// <returns></returns>
    public float[] ConvertStereoSamplestoMono(float[] samples) 
    {
        var monoSamples = new float[samples.Length / 2]; // stereo samples have two data points for every sample (L+R track)
                                                         // so mono will have half the number of samples
        for (var i = 0; i < samples.Length / 2; i ++)
        {
            monoSamples[i / 2] = (samples[i*2] + samples[i*2 + 1]) / 2; // average the two stereo samples to get a mono sample
        }
        return monoSamples;
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

using System;
using UnityEngine;
using Un4seen.Bass;
using System.Collections.Generic;

public class PluginBassManager : MonoBehaviour
{
    int sampleRate = 44100;

    /// <summary>
    /// Holds a value in seconds for how often to take a sample from all samples
    /// <para>1 millisecond (0.001 seconds) by default.</para>
    /// </summary>
    public float CompressedArrayResolution {get; private set;}

    /// <summary>
    /// Holds BASS stream data for playing audio. Stem is audio stem identifier, int is stream data.
    /// </summary>
    public Dictionary<ChartMetadata.StemType, int> StemStreams {get; private set;}
    
    string testSongPath = "G:/_PCE_files/TestAudioFiles/120BPMTestTrack.opus";
    
    /// <summary>
    /// Is the audio file currently playing?
    /// </summary>
    public bool AudioPlaying {get; private set;}

    WaveformManager waveformManager;

    private void Awake() 
    {
        StemStreams = new();
        AudioPlaying = false;
        CompressedArrayResolution = 0.001f;
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();

        InitializeBassPlugin();
        testSongPath = "G:/_PCE_files/TestAudioFiles/120BPMTestTrack.opus";
        UpdateAudioStream(ChartMetadata.StemType.song, testSongPath);
    }

    public void UpdateAudioStream(ChartMetadata.StemType stemType, string songPath)
    {
        // Make this asynchronous for later? idk
        if (StemStreams.ContainsKey(stemType))
        {
            Bass.BASS_StreamFree(StemStreams[stemType]); // I think I have to do this to prevent memory leaks? Just doing this to be cautious
            StemStreams.Remove(stemType); // Flush current value in case user updates the directory, avoids error
        }
        StemStreams.Add(stemType, Bass.BASS_StreamCreateFile(songPath, 0, 0, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_PRESCAN));
        // Create master stream in data set to avoid creating a stream over and over during frequent start/stopping
    }

    void InitializeBassPlugin()
    {
        if (Bass.BASS_Init(-1, sampleRate, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
        {
            Bass.BASS_PluginLoadDirectory($"{Application.dataPath}/Plugins/Bassx64");
        }
        else
        {
            Debug.Log($"Init failed");
        }
    }

    /// <summary>
    /// Simplify an audio file into x samples taken every CompressedArrayResolution milliseconds from the audio file.
    /// </summary>
    /// <param name="songPath">File explorer path to the audio file.</param>
    /// <param name="bytesPerSample">Number of bytes in the original track that each sample represents. Can vary based on encoding.</param>
    /// <returns>Float array of an audio file's sample data.</returns>
    /// <exception cref="ArgumentException">Invalid song path</exception>
    public float[] GetAudioSamples(string songPath, out long bytesPerSample) // testing path as default value
    {
        // Step 1: Make BASS stream of song path
        songPath = testSongPath;
        // Use ChartMetadata.StemTypes & stem dictionary in future

        // GetAudioSamples() uses a different one-time stream from stemStreams{} because it needs decoded stream
        var currentTrackStream = Bass.BASS_StreamCreateFile(
            songPath, 
            0, 0, 
            BASSFlag.BASS_SAMPLE_FLOAT |
            BASSFlag.BASS_STREAM_DECODE | 
            BASSFlag.BASS_STREAM_PRESCAN
        ); // Loads in file stream to get waveform data from

        // Step 1a: Make sure the track is valid (improve notification system later pls)
        if (currentTrackStream == 0)
        {
            throw new ArgumentException("File could not be loaded");
        }

        // Step 2: Calculate how long the song is and how many samples it will provide
        var songLengthBytes = Bass.BASS_ChannelGetLength(currentTrackStream); // Get # of bytes in song
        var floatArrayLength = songLengthBytes/4; // # of vals in float[] array will be 1/4 of this bc 4 bytes per 32 bit float

        // Step 3: Get an array of the samples from the BASS stream
        float[] allSamples = new float[floatArrayLength];
        var x = Bass.BASS_ChannelGetData(currentTrackStream, allSamples, (int)songLengthBytes); // Get an array of every single sample
        // This is faster than taking individual samples using setPosition and then ChannelGetData

        // Step 4: Convert the stereo track to mono so that the waveform is taking samples from an average of both channels
        // Default BASS mono flag only works with MP3 files so a manual function is needed
        allSamples = ConvertStereoSamplestoMono(allSamples);

        // Step 5: Calculate number of samples to take and how long each sample is in bytes
        long sampleIntervalBytes = Bass.BASS_ChannelSeconds2Bytes(currentTrackStream, CompressedArrayResolution) / 8; // Number of bytes in x seconds of audio (/4) - div by extra 2 because converted to mono audio
        bytesPerSample = sampleIntervalBytes * 4; // multiply by 4 to undo /2 for floats and /2 for mono 
        // ^ this is used to play/pause the audio which uses 16-bit (not 32-bit) & stereo
        // ^ this is *4 and not *8 because 16 bit is still 2 bytes
        var compArraySize = (int)Math.Floor((double)songLengthBytes / sampleIntervalBytes) / 8; // Number of samples to compress down to for line rendering

        // Step 6: Pick data from full array to put in compressed array
        float[] waveformData = new float[compArraySize]; // Array of vals to hold compressed data
        for (var i = 0; i < compArraySize; i++)
        {
            waveformData[i] = Math.Abs(allSamples[i * sampleIntervalBytes]); // Select abs val of sample every 1 ms from all the samples and store it in the compressed array
        }

        // Step 7: Free up the stream to prevent memory leaks because BASS uses unmanaged code
        Bass.BASS_StreamFree(currentTrackStream); 
        // Data is no longer needed now that it has been processed in waveformData

        return waveformData;
    }

    public void PlayPauseAudio()
    {
        Bass.BASS_ChannelSetPosition
        (
            StemStreams[ChartMetadata.StemType.song], 
            WaveformManager.CurrentWFDataPosition * WaveformManager.WaveformData[ChartMetadata.StemType.song].Item2
            // ^ Item2 holds how many bytes are held for each second of audio
            // this rate will vary based on audio formats and stuff
        );
        if (!AudioPlaying)
        {
            Bass.BASS_ChannelPlay(StemStreams[ChartMetadata.StemType.song], false);
            AudioPlaying = true;
        }
        else
        {
            Bass.BASS_ChannelPause(StemStreams[ChartMetadata.StemType.song]);
            AudioPlaying = false;
            waveformManager.ResetAudioPositions();
            waveformManager.ScrollWaveformSegment(0, false);
        }
        waveformManager.ToggleCharting();
    }

    /// <summary>
    /// Converts stereo samples to mono samples to get a more accurate waveform.
    /// </summary>
    /// <param name="samples"></param>
    /// <returns></returns>
    private float[] ConvertStereoSamplestoMono(float[] samples) 
    {
        var monoSamples = new float[samples.Length / 2]; // stereo samples have two data points for every sample (L+R track)
                                                         // so mono will have half the number of samples
        for (var i = 0; i < monoSamples.Length; i++)
        {
            monoSamples[i] = (samples[i*2] + samples[i*2 + 1]) / 2; // average both stereo samples
        }
        return monoSamples;
    }

    void OnApplicationQuit()
    {
        Bass.BASS_Free();     
    }
}

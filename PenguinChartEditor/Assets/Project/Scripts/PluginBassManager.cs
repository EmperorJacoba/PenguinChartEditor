using System;
using UnityEngine;
using Un4seen.Bass;
using System.Collections.Generic;

public class PluginBassManager : MonoBehaviour
{
    int sampleRate = 44100;
    public float compressedArrayResolution = 0.001f; // Holds a value in seconds for how often to take a sample from all samples 
                                                    // needs to be reeeeeally small for a good waveform - currently too big
                                                    // Maybe just do it directly in bytes later on?
    string testSongPath = "G:/_PCE_files/TestAudioFiles/song.opus";
    public bool audioPlaying = false;

    WaveformManager waveformManager;

    private void Awake() 
    {
        InitializeBassPlugin();
        testSongPath = "G:/_PCE_files/TestAudioFiles/song.opus";
        UpdateAudioStream(ChartMetadata.StemType.song, testSongPath);

        waveformManager = GameObject.Find("WaveformManager").gameObject.GetComponent<WaveformManager>();
    }

    public void UpdateAudioStream(ChartMetadata.StemType stemType, string songPath)
    {
        // Make this asynchronous for later? idk
        if (stemStreams.ContainsKey(stemType))
        {
            Bass.BASS_StreamFree(stemStreams[stemType]); // I think I have to do this to prevent memory leaks? Just doing this to be cautious
            stemStreams.Remove(stemType); // Flush current value in case user updates the directory, avoids error
        }
        stemStreams.Add(stemType, Bass.BASS_StreamCreateFile(songPath, 0, 0, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_PRESCAN));
        // Create master stream in data set to avoid creating a stream over and over during frequent start/stopping
    }

    public Dictionary<ChartMetadata.StemType, int> stemStreams = new(); 

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
    /// Returns a simplified float array of an audio file's sample data.
    /// </summary>
    /// <param name="songPath"></param>
    /// <param name="normalizationFactor">This number decreases the length of the peaks of the waveform
    /// Since the line renderer uses local positioning for easier culling & positioning, it looks super crazy without dividing by a</param>
    /// <returns>waveformData (float[]) that contains selected audio samples.</returns>
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
        long sampleIntervalBytes = Bass.BASS_ChannelSeconds2Bytes(currentTrackStream, compressedArrayResolution) / 8; // Number of bytes in x seconds of audio (/4) - div by extra 2 because converted to mono audio
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

    // This function has not yet been tested!!
    public void PlayPauseAudio()
    {
        Bass.BASS_ChannelSetPosition
        (
            stemStreams[ChartMetadata.StemType.song], 
            WaveformManager.currentWFDataPosition * WaveformManager.waveformData[ChartMetadata.StemType.song].Item2
            // ^ Item2 holds how many bytes are held for each second of audio
            // this rate will vary based on audio formats and stuff
        );
        if (!audioPlaying)
        {
            Bass.BASS_ChannelPlay(stemStreams[ChartMetadata.StemType.song], false);
            audioPlaying = true;
            waveformManager.ToggleCharting();
            waveformManager.ChunkWaveformSegment();
        }
        else
        {
            Bass.BASS_ChannelPause(stemStreams[ChartMetadata.StemType.song]);
            audioPlaying = false;
            waveformManager.ToggleCharting();
        }

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

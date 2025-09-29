using System;
using UnityEngine;
using Un4seen.Bass;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    #region Properties

    const int SAMPLE_RATE = 44100;
    public const float MAX_VOLUME = 1;

    /// <summary>
    /// Holds a value in seconds for how often to take a sample from all samples (1 millisecond)
    /// </summary>
    public const double ARRAY_RESOLUTION = 0.001;

    /// <summary>
    /// The amount of audio samples in the compressed array that exist for every second in the audio.
    /// </summary>
    public const int SAMPLES_PER_SECOND = 1000; // I don't want to mess with floating point 1/x garbo so this is here

    /// <summary>
    /// Holds BASS stream data for playing audio. Stem is audio stem identifier, int is BASS stream data.
    /// </summary>
    public static Dictionary<Metadata.StemType, int> StemStreams { get; private set; } = new();

    public static Dictionary<Metadata.StemType, StemVolumeData> StemVolumes { get; private set; } = new();
    public static HashSet<Metadata.StemType> soloedStems = new();

    /// <summary>
    /// Is the audio currently playing?
    /// </summary>
    public static bool AudioPlaying
    {
        get
        {
            return _playing;
        }
        set
        {
            if (value == _playing) return;
            _playing = value;
            BeatlinePreviewer.editMode = !_playing;
            PlaybackStateChanged?.Invoke(_playing);
        }
    }
    private static bool _playing = false;

    public delegate void PlayingDelegate(bool state);

    /// <summary>
    /// Event that fires whenever the song playing state changes.
    /// </summary>
    public static event PlayingDelegate PlaybackStateChanged;

    /// <summary>
    /// The stem with the longest stream length in StemStreams. All other stem streams are linked to this stem for playback purposes.
    /// <para>This stream is guaranteed to exist in StemStreams at all times EXCEPT when there is no audio loaded.</para> 
    /// </summary>
    private static Metadata.StemType StreamLink { get; set; }

    /// <summary>
    /// The length of the stream attached to the longest stem.
    /// </summary>
    public static float SongLength { get; set; }

    #endregion

    #region Unity Functions
    private void Awake()
    {
        InitializeBassPlugin();

        // streams are only updated in Song Setup so this data will remain the same throughout entire scene usage
        UpdateStemStreams();

        StreamLink = GetLongestStream();
        LinkStreams();
    }

    void OnApplicationQuit()
    {
        Bass.BASS_Free();
    }

    #endregion

    #region Audio Setup

    void InitializeBassPlugin()
    {
        if (Bass.BASS_Init(-1, SAMPLE_RATE, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
        {
            Bass.BASS_PluginLoadDirectory($"{Application.dataPath}/Plugins/Bassx64");
        }
        else
        {
            Debug.Log($"Init failed");
        }
    }

    /// <summary>
    /// Simplify an audio file into x samples taken every ArrayResolution milliseconds from the audio file.
    /// </summary>
    /// <param name="songPath">File explorer path to the audio file.</param>
    /// <param name="bytesPerSample">Number of bytes in the original track that each sample represents. Can vary based on encoding.</param>
    /// <returns>Float array of an audio file's sample data.</returns>
    /// <exception cref="ArgumentException">Invalid song path</exception>
    public float[] GetAudioSamples(Metadata.StemType stem, out long bytesPerSample)
    {
        // Step 1: Make BASS stream of song path
        var songPath = Chart.Metadata.Stems[stem];

        // GetAudioSamples() uses a different one-time stream from stemStreams{} because it needs BASS_STREAM_DECODE flag to get data
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
        var floatArrayLength = songLengthBytes / 4; // # of vals in float[] array will be 1/4 of this bc 4 bytes per 32 bit float

        // Step 3: Get an array of the samples from the BASS stream
        float[] allSamples = new float[floatArrayLength];
        var x = Bass.BASS_ChannelGetData(currentTrackStream, allSamples, (int)songLengthBytes); // Get an array of every single sample
        // This is faster than taking individual samples using setPosition and then ChannelGetData

        // Step 4: Convert the stereo track to mono so that the waveform is taking samples from an average of both channels
        // Default BASS mono flag only works with MP3 files so a manual function is needed
        allSamples = ConvertStereoSamplestoMono(allSamples);

        // Step 5: Calculate number of samples to take and how long each sample is in bytes
        long sampleIntervalBytes = Bass.BASS_ChannelSeconds2Bytes(currentTrackStream, ARRAY_RESOLUTION) / 8; // Number of bytes in x seconds of audio (/4) - div by extra 2 because converted to mono audio
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

    /// <summary>
    /// Converts stereo samples to mono samples to get a more accurate waveform.
    /// </summary>
    /// <param name="stereoSamples"></param>
    /// <returns></returns>
    private float[] ConvertStereoSamplestoMono(float[] stereoSamples)
    {
        var monoSamples = new float[stereoSamples.Length / 2]; // stereo samples have two data points for every sample (L+R track)
                                                               // so mono will have half the number of samples
        for (var i = 0; i < monoSamples.Length; i++)
        {
            monoSamples[i] = (stereoSamples[i * 2] + stereoSamples[i * 2 + 1]) / 2; // average both stereo samples
        }
        return monoSamples;
    }

    /// <summary>
    /// Generate BASS streams from file paths in Stem dict in ChartMetadata.
    /// </summary>
    void UpdateStemStreams()
    {
        StemStreams.Clear();
        StemVolumes.Clear();
        foreach (var stem in Chart.Metadata.Stems)
        {
            try
            {
                UpdateAudioStream(stem.Key, stem.Value);
            }
            catch
            {
                continue;
            }
        }
    }

    /// <summary>
    /// Create BASS stream from a file path with a StemType identifier.
    /// </summary>
    /// <param name="stemType">The stem that the BASS stream belongs to.</param>
    /// <param name="songPath">The file path to create a stream from.</param>
    /// <exception cref="ArgumentException">Thrown when created stem returns an error (0) from BASS</exception>
    public void UpdateAudioStream(Metadata.StemType stemType, string songPath)
    {
        // Make this asynchronous for later? idk
        // Create master stream in data set to avoid creating a stream over and over during frequent start/stopping
        if (StemStreams.ContainsKey(stemType))
        {
            Bass.BASS_StreamFree(StemStreams[stemType]); // I think I have to do this to prevent memory leaks? Just doing this to be cautious
            StemStreams.Remove(stemType); // Flush current value just in case
        }

        if (!StemVolumes.ContainsKey(stemType))
        {
            StemVolumes.Add(stemType, new(MAX_VOLUME, false));
        }

        StemStreams.Add(stemType, Bass.BASS_StreamCreateFile(songPath, 0, 0, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_PRESCAN));

        if (StemStreams[stemType] == 0) // this is here instead of above to avoid creating 2 streams and only being able to release one of them (which would cause a memory leak) 
        {
            throw new ArgumentException($"Bad song stem passed into stream update. Try reloading directory or choosing new file. Debug: (Stem: {stemType})");
        }
    }

    /// <summary>
    /// Link all BASS streams to the longest BASS stream for playback purposes.
    /// </summary>
    private void LinkStreams()
    {
        // In order to play all the streams in sync they must be linked together
        // All audio must be linked to the longest stream so that pausing the longer stream is possible after other shorter ones have ended
        foreach (var stream in StemStreams)
        {
            Bass.BASS_ChannelSetLink(StemStreams[StreamLink], stream.Value);
        }
    }

    /// <summary>
    /// Get the longest audio file to link playing all other stems to. Needed to properly play audio synchronously.
    /// </summary>
    /// <returns>Stem with longest playback length</returns>
    private Metadata.StemType GetLongestStream()
    {
        // Basic max value finder algorithm: get length of each stem, overwrite current longest stem if new longest is found
        long streamLength = 0;
        Metadata.StemType longestStream = 0; // if this function returns 0 then it shows nothing has been loaded
        foreach (var stream in StemStreams)
        {
            var currentStreamLength = Bass.BASS_ChannelGetLength(stream.Value);
            if (currentStreamLength > streamLength)
            {
                streamLength = currentStreamLength;
                longestStream = stream.Key;
            }
        }
        SongLength = (float)Bass.BASS_ChannelBytes2Seconds(StemStreams[longestStream], streamLength);
        return longestStream;
    }

    #endregion

    #region Audio Editing

    public static void ChangeAudioSpeed(float newSpeed)
    {
        foreach (var stream in StemStreams)
        {
            Bass.BASS_ChannelSetAttribute(StemStreams[stream.Key], BASSAttribute.BASS_ATTRIB_FREQ, Bass.BASS_ChannelGetInfo(StemStreams[stream.Key]).freq * newSpeed);
        }
    }

    public static void MuteStem(Metadata.StemType stem)
    {
        StemVolumes[stem] = new(StemVolumes[stem].Volume, true);
        Bass.BASS_ChannelSetAttribute(StemStreams[stem], BASSAttribute.BASS_ATTRIB_VOL, 0);
    }

    public static void UnmuteStem(Metadata.StemType stem)
    {
        StemVolumes[stem] = new(StemVolumes[stem].Volume, false);
        Bass.BASS_ChannelSetAttribute(StemStreams[stem], BASSAttribute.BASS_ATTRIB_VOL, StemVolumes[stem].Volume);
    }

    public static void SetStemVolume(Metadata.StemType stem, float newVolume)
    {
        StemVolumes[stem] = new(newVolume, StemVolumes[stem].Muted);

        if (StemVolumes[stem].Muted) return;
        Bass.BASS_ChannelSetAttribute(StemStreams[stem], BASSAttribute.BASS_ATTRIB_VOL, newVolume);
    }

    /// <summary>
    /// Called by user playing/pausing the audio from a button.
    /// </summary>
    public void PlayPauseAudio()
    {
        if (!AudioPlaying)
        {
            PlayAudio();
        }
        else
        {
            PauseAudio();
        }
    }

    public void PlayAudio()
    {
        if (!AudioPlaying)
        {
            SetStreamPositions();
            Bass.BASS_ChannelPlay(StemStreams[StreamLink], false);
            AudioPlaying = true;
            SongTimelineManager.ToggleChartingInputMap();
        }
    }

    public void PauseAudio()
    {
        if (AudioPlaying)
        {
            Bass.BASS_ChannelPause(StemStreams[StreamLink]);
            AudioPlaying = false;
            SongTimelineManager.ToggleChartingInputMap();
        }
    }

    public void StopAudio()
    {
        SongTimelineManager.SongPositionSeconds = 0;
        if (AudioPlaying)
        {
            Bass.BASS_ChannelPause(StemStreams[StreamLink]);
            SetStreamPositions();
            AudioPlaying = false;
            SongTimelineManager.ToggleChartingInputMap();
        }
    }

    static int metronomeStreamHandle = -1;
    public static void PlayMetronomeSound()
    {
        if (metronomeStreamHandle == -1)
        {
            metronomeStreamHandle = Bass.BASS_StreamCreateFile($"{Application.streamingAssetsPath}/metronomeclick.mp3", 0, 0, BASSFlag.BASS_DEFAULT);
        }
        Bass.BASS_ChannelPlay(metronomeStreamHandle, false);
    }

    /// <summary>
    /// Set the stream position to the waveform's current position for every stream in StemStreams. 
    /// </summary>
    private void SetStreamPositions()
    {
        foreach (var streampair in StemStreams)
        {
            try
            {
                Bass.BASS_ChannelSetPosition
                (
                    StemStreams[streampair.Key],
                    SongTimelineManager.SongPositionSeconds
                );
            }
            catch
            {
                continue; // this is when audio file lengths differ - figure out what to do here
            }
        }
    }

    /// <summary>
    /// Get the current audio position of the main audio playback stem.
    /// </summary>
    /// <returns></returns>
    public static double GetCurrentAudioPosition()
    {
        return Bass.BASS_ChannelBytes2Seconds(StemStreams[StreamLink], Bass.BASS_ChannelGetPosition(StemStreams[StreamLink]));
    }

    #endregion
    // need to free streams when switching to different tabs too
}

public struct StemVolumeData
{
    public float Volume;
    public bool Muted;
    public StemVolumeData(float volume, bool muted)
    {
        Volume = volume;
        Muted = muted;
    }
}
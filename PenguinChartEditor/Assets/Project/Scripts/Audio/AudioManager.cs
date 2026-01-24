using System;
using UnityEngine;
using Un4seen.Bass;
using System.Collections.Generic;

// This file is based around the licensed product plugin BASS and its .NET wrapper, BASS.NET.
// You must obtain your own license of BASS and BASS.NET if you would like to repackage the code in this file by the terms each program specifies.
// Information on licensing BASS: https://www.un4seen.com/bass.html | Information on licensing BASS.NET: https://www.radio42.com/bass/bass_register.html
// The license for PenguinChartEditor for BASS.NET is held by EmperorJacoba. PCE is licensed under freeware terms.
// Documentation about BASS is available here: https://www.radio42.com/bass/help/ (Overview will help the most)
public class AudioManager : MonoBehaviour
{
    #region Properties

    private const int SAMPLE_RATE = 44100;
    public const float MAX_VOLUME = 1;

    /// <summary>
    /// Holds a value in seconds for how often to take a sample from all samples (1 millisecond)
    /// </summary>
    public const double ARRAY_RESOLUTION = 0.001;

    /// <summary>
    /// The amount of audio samples in the compressed array that exist for every second in the audio.
    /// </summary>
    public const int SAMPLES_PER_SECOND = 1000; // I don't want to mess with floating point 1/x garbage so this is here

    /// <summary>
    /// Holds BASS stream data for playing audio. Stem is audio stem identifier, int is BASS stream data.
    /// </summary>
    public static Dictionary<StemType, int> StemStreams { get; private set; } = new();

    public static Dictionary<StemType, StemVolumeData> StemVolumes { get; private set; } = new();
    public static HashSet<StemType> soloedStems = new();

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
            //BeatlinePreviewer.editMode = !_playing;
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
    private static StemType StreamLink { get; set; }

    /// <summary>
    /// The length of the stream attached to the longest stem.
    /// </summary>
    public static float SongLength { get; set; } = 0;

    #endregion

    #region Unity Functions

    private InputMap inputMap;
    private void Awake()
    {
        inputMap = new InputMap();
        inputMap.Enable();
        inputMap.ExternalCharting.PlayPause.performed += x => ToggleAudioPlayback();
    }

    public static void InitializeAudio()
    {
        UpdateStemStreams();

        StreamLink = GetLongestStream();
        LinkStreams();
    }

    private void OnApplicationQuit()
    {
        Bass.BASS_Free();
    }

    #endregion

    #region Audio Setup

    public static void InitializeBassPlugin()
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
    /// <returns>Compressed float array of an audio file's sample data.</returns>
    /// <exception cref="ArgumentException">Invalid song path</exception>
    public static float[] GetAudioSamples(StemType stem, out long bytesPerSample)
    {
        var songPath = Chart.Metadata.StemPaths[stem];

        // GetAudioSamples() uses a different one-time stream from stemStreams{} because it needs BASS_STREAM_DECODE flag to get data
        var currentTrackStream = Bass.BASS_StreamCreateFile(
            songPath,
            0, 0,
            BASSFlag.BASS_SAMPLE_FLOAT |
            BASSFlag.BASS_STREAM_DECODE |
            BASSFlag.BASS_STREAM_PRESCAN
        );

        if (currentTrackStream == 0) throw new ArgumentException($"File {stem} could not be loaded");

        var songLengthBytes = Bass.BASS_ChannelGetLength(currentTrackStream);

        long sampleIntervalBytes = Bass.BASS_ChannelSeconds2Bytes(currentTrackStream, ARRAY_RESOLUTION) / sizeof(float);
        bytesPerSample = sampleIntervalBytes * 2;

        var compressedArraySize = (int)Math.Floor((double)songLengthBytes / sampleIntervalBytes) / sizeof(float);

        float[] waveformData = new float[compressedArraySize]; // Array of vals to hold compressed data

        long bytesUnread = songLengthBytes;
        long currentWaveformDataPosition = 0;
        var buffer = Bass.BASS_ChannelSeconds2Bytes(currentTrackStream, 10);

        while (bytesUnread > 0)
        {
            var bytesToRead = Math.Min(buffer, bytesUnread);

            float[] stereoSamples = new float[bytesToRead / sizeof(float)];
            Bass.BASS_ChannelGetData(currentTrackStream, stereoSamples, (int)bytesToRead);

            int sample;
            for (sample = 0; sample * sampleIntervalBytes + 1 < stereoSamples.Length && currentWaveformDataPosition + sample < waveformData.Length; sample++)
            {
                var averagedMonoSample = (stereoSamples[sample * sampleIntervalBytes] + stereoSamples[sample * sampleIntervalBytes + 1]) / 2;
                waveformData[currentWaveformDataPosition + sample] = Math.Abs(averagedMonoSample);
            }

            bytesUnread -= bytesToRead;
            currentWaveformDataPosition += sample;
            Bass.BASS_ChannelSetPosition(currentTrackStream, songLengthBytes - bytesUnread, BASSMode.BASS_POS_BYTE); // length - unread = read
        }

        // BASS is unmanaged; memory must be manually freed
        Bass.BASS_StreamFree(currentTrackStream);
        return waveformData;
    }

    /// <summary>
    /// Generate BASS streams from file paths in Stem dict in ChartMetadata.
    /// </summary>
    private static void UpdateStemStreams()
    {
        StemStreams.Clear();
        StemVolumes.Clear();
        foreach (var stem in Chart.Metadata.StemPaths)
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
    /// Create BASS stream from a file path with a StemType iwakedentifier.
    /// </summary>
    /// <param name="stemType">The stem that the BASS stream belongs to.</param>
    /// <param name="songPath">The file path to create a stream from.</param>
    /// <exception cref="ArgumentException">Thrown when created stem returns an error (0) from BASS</exception>
    private static void UpdateAudioStream(StemType stemType, string songPath)
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
            StemVolumes.Add(stemType, new StemVolumeData(MAX_VOLUME, false));
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
    private static void LinkStreams()
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
    private static StemType GetLongestStream()
    {
        // Basic max value finder algorithm: get length of each stem, overwrite current longest stem if new longest is found
        long streamLength = 0;
        StemType longestStream = 0; // if this function returns 0 then it shows nothing has been loaded
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

    public static float currentAudioSpeed = 1;
    public static void ChangeAudioSpeed(float newSpeed)
    {
        currentAudioSpeed = newSpeed;
        foreach (var stream in StemStreams)
        {
            Bass.BASS_ChannelSetAttribute(StemStreams[stream.Key], BASSAttribute.BASS_ATTRIB_FREQ, Bass.BASS_ChannelGetInfo(StemStreams[stream.Key]).freq * newSpeed);
        }
    }

    public static void MuteStem(StemType stem)
    {
        StemVolumes[stem] = new StemVolumeData(StemVolumes[stem].Volume, true);
        Bass.BASS_ChannelSetAttribute(StemStreams[stem], BASSAttribute.BASS_ATTRIB_VOL, 0);
    }

    public static void UnmuteStem(StemType stem)
    {
        StemVolumes[stem] = new StemVolumeData(StemVolumes[stem].Volume, false);
        Bass.BASS_ChannelSetAttribute(StemStreams[stem], BASSAttribute.BASS_ATTRIB_VOL, StemVolumes[stem].Volume);
    }

    public static void SetStemVolume(StemType stem, float newVolume)
    {
        StemVolumes[stem] = new StemVolumeData(newVolume, StemVolumes[stem].Muted);

        if (StemVolumes[stem].Muted) return;
        Bass.BASS_ChannelSetAttribute(StemStreams[stem], BASSAttribute.BASS_ATTRIB_VOL, newVolume);
    }

    /// <summary>
    /// Called by user playing/pausing the audio from a button.
    /// </summary>
    public static void ToggleAudioPlayback()
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

    public static void PlayAudio()
    {
        if (!AudioPlaying)
        {
            SetStreamPositions();
            Bass.BASS_ChannelPlay(StemStreams[StreamLink], false);
            AudioPlaying = true;
            SongTime.DisableChartingInputMap();
        }
    }

    public static void PauseAudio()
    {
        if (AudioPlaying)
        {
            Bass.BASS_ChannelPause(StemStreams[StreamLink]);
            AudioPlaying = false;
            SongTime.EnableChartingInputMap();
        }
    }

    public static void StopAudio()
    {
        SongTime.SongPositionSeconds = 0;
        if (AudioPlaying)
        {
            Bass.BASS_ChannelPause(StemStreams[StreamLink]);
            SetStreamPositions();
            AudioPlaying = false;
            SongTime.ToggleChartingInputMap();
        }
    }

    private static int metronomeStreamHandle = -1;
    public static void PlayMetronomeSound()
    {
        if (metronomeStreamHandle == -1)
        {
            metronomeStreamHandle = Bass.BASS_StreamCreateFile($"{Application.streamingAssetsPath}/metronomeclick.mp3", 0, 0, BASSFlag.BASS_DEFAULT);
        }
        bool success = Bass.BASS_ChannelPlay(metronomeStreamHandle, false);
    }

    private static int clapStreamHandle = -1;

    public static void PlayClapSound()
    {
        if (clapStreamHandle == -1)
        {
            clapStreamHandle = Bass.BASS_StreamCreateFile($"{Application.streamingAssetsPath}/clap.mp3", 0, 0, BASSFlag.BASS_DEFAULT);
        }
        bool success = Bass.BASS_ChannelPlay(clapStreamHandle, false);
    }

    /// <summary>
    /// Set the stream position to the waveform's current position for every stream in StemStreams. 
    /// </summary>
    private static void SetStreamPositions()
    {
        foreach (var streampair in StemStreams)
        {
            try
            {
                Bass.BASS_ChannelSetPosition
                (
                    StemStreams[streampair.Key],
                    SongTime.SongPositionSeconds
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
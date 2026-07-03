using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ManagedBass;
using ManagedBass.Enc;
using Penguin.Debug;
using UnityEngine;
using UnityEngine.InputSystem;

// This file is based around the licensed product plugin BASS, interacting through the ManagedBass plugin.
// You must obtain your own license of BASS if you would like to repackage the code in this file by the terms each program specifies.
// Information on licensing BASS: https://www.un4seen.com/bass.html

// FIXME: Make this a static class OR a Chart object (probably the better choice)
public class AudioManager : MonoBehaviour
{
    private const double BUFFER_SIZE = 10.0;
    
    // These two constants are inverses of each other. When changing these, please
    // update to match.
    public const double ARRAY_RESOLUTION = 0.001;
    public const int SAMPLES_PER_SECOND = 1000;

    /// <summary>
    /// The stem with the longest stream length in StemStreams.
    /// All other stem streams are linked to this stem for playback purposes.
    /// <remarks>
    /// This stream is guaranteed to exist in StemStreams at all times EXCEPT when there is no audio loaded,
    /// when it is placeholder_silence.opus. As a placeholder, it will not be in Stems but will still be playable.
    /// </remarks> 
    /// </summary>
    private static BassStream StreamLink
    {
        get => _l;
        set
        {
            _l = value;
            if (_l is null)
            {
                Debug.LogWarning("StreamLink is being set to null. This may cause issues with playback.");
                SongLength = 0;
                return;
            }
            
            SongLength = _l.TimeLength;

            foreach (var stream in Streams.Values)
            {
                stream.LinkTo(value);
            }
        } 
    }
    private static BassStream _l;
    
    /// <remarks>
    /// Don't use .Clear(). Assign to a new dict every time to make sure all the streams are freed first.
    /// </remarks>
    private static Dictionary<StemType, BassStream> Streams
    {
        get => _streams;
        set
        {
            foreach (var stream in Streams.Values) stream.Free();
            _streams = value;
        }
    }
    private static Dictionary<StemType, BassStream> _streams = new();
    public static bool IsAudioLoaded() => Streams.Count > 0;
    
    // Cached b/c stream link is not changed much. Just make sure to update this when stream link changes.
    public static double SongLength
    {
        get
        {
            return _sL;
        }
        private set => _sL = value;
    }

    private static double _sL = 0;

    public delegate void PlayingDelegate(bool state);
    public static event PlayingDelegate PlaybackStateChanged;
    
    public static bool AudioPlaying
    {
        get => _playing;
        private set
        {
            if (value == _playing) return;
            
            _playing = value;
            
            PlaybackStateChanged?.Invoke(_playing);
        }
    }
    private static bool _playing;

    public static double AudioPosition
    {
        get => StreamLink.AudioPosition - Chart.settings.Calibration;
        set => SetStreamPositions(value);
    }

    public static float AudioSpeed
    {
        get => _ps;
        set
        {
            if (_ps <= 0) _ps = 0.01f;
            _ps = value;
            
            foreach (var stream in Streams.Values)
            {
                stream.PlaySpeed = _ps;
            }
        }
    }
    private static float _ps = 1;

    public static double MasterVolume
    {
        get => _mv;
        set
        {
            if (value > 1) value = 1;
            if (value < 0) value = 0;
            _mv = value;

            foreach (var stream in Streams.Values)
            {
                stream.RefreshInternalVolume();
            }
        }
    }

    private static double _mv = 1;
    
    public static void Initialize()
    {
        if (!Bass.Init())
        {
            Debug.LogError($"Could not load BASS plugin. {Bass.LastError}");
            return;
        }
        
        string pluginPath = $"{Application.dataPath}/Plugins/";

#if UNITY_EDITOR_WIN && UNITY_EDITOR
        pluginPath += "Bass/Bass_win";
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        path += "x86_64";
#endif 
        // fix: these file paths are not valid in standalone builds
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        path += "Bass_macOS";
#endif
#if (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        path += "Bass_linux/x86_64";
#endif
        
        foreach (var file in Directory.EnumerateFiles(pluginPath))
        {
            if (file.Contains("meta")) continue;
            var fileName = Path.GetFileName(file);
            if (fileName == "bass.dll" || fileName.Contains("bassenc") || fileName.Contains("bassmix")) continue;

            if (Bass.PluginLoad(file) != 0) continue;
            if (Bass.LastError == Errors.Already) continue;

            Debug.LogWarning($"Plugin Load error for {file}. Bass Error: {Bass.LastError}");
        }
        
        metronome = new BassStream($"{Application.streamingAssetsPath}/metronomeclick.opus");
        clap = new BassStream($"{Application.streamingAssetsPath}/clap.opus");
        placeholder = new BassStream($"{Application.streamingAssetsPath}/placeholder_silence.opus");
        StreamLink = placeholder;
    }
    
    private void Awake()
    {
        Chart.inputMap.UIShortcuts.PlayPause.performed += ToggleAudioPlayback;

        SceneTabSwitcher.TabChanged += PauseAudio;
    }
    
    
    public static void DisableAudioPlaybackControls() => Chart.inputMap.UIShortcuts.Disable();
    public static void EnableAudioPlaybackControls() => Chart.inputMap.UIShortcuts.Enable();

    private void ToggleAudioPlayback(InputAction.CallbackContext _)
    {
        if (Chart.LoadedInstrument is null) return;
        
        if (AudioPlaying) PauseAudio();
        else PlayAudio();
    }

    private void OnDestroy()
    {
        Chart.inputMap.UIShortcuts.PlayPause.performed -= ToggleAudioPlayback;
    }

    private void OnApplicationQuit()
    {
        Bass.Free();
    }

    /// <summary>
    /// Simplify an audio file into x samples taken every ArrayResolution milliseconds from the audio file.
    /// </summary>
    /// <remarks>
    /// Currently gets ALL audio samples in the chart for the waveform. Obviously bad. Please use buffering.
    /// </remarks>
    public static float[] GetAllAudioSamples(StemType stem)
    {
        var streamHandle = CreateDecodedStream(Chart.Metadata.StemPaths[stem]);

        if (streamHandle == 0)
        {
            Debug.LogError($"Aborting waveform gen. " +
                           $"Bass error when loading decoded stream from " +
                           $"stem {stem} with file path: {Chart.Metadata.StemPaths[stem]}. {Bass.LastError}");
            return Array.Empty<float>();
        }
        
        var songLengthBytes = Bass.ChannelGetLength(streamHandle);
        var sampleIntervalBytes = Bass.ChannelSeconds2Bytes(streamHandle, ARRAY_RESOLUTION) / sizeof(float);
        var arraySize = (int)Math.Floor((double)songLengthBytes / sampleIntervalBytes) / sizeof(float);
        
        var waveformData = new float[arraySize];
        
        var bytesUnread = songLengthBytes;
        var currentCumulativeSamplePos = 0L;
        var buffer = Bass.ChannelSeconds2Bytes(streamHandle, BUFFER_SIZE);
        
        while (bytesUnread > 0)
        {
            var bytesThisPass = Math.Min(buffer, bytesUnread);

            var stereoSamples = new float[bytesThisPass / sizeof(float)];
            Bass.ChannelGetData(streamHandle, stereoSamples, (int)bytesThisPass);
            
            int sample;
            for (
                    sample = 0; 
                    sample * sampleIntervalBytes + 1 < stereoSamples.Length && 
                    currentCumulativeSamplePos + sample < waveformData.Length; 
                    sample++
                )
            {
                var averagedMonoSample = 
                    (stereoSamples[sample * sampleIntervalBytes] + 
                     stereoSamples[sample * sampleIntervalBytes + 1]) 
                    / 2;
                
                // abs for symmetry
                waveformData[currentCumulativeSamplePos + sample] = Math.Abs(averagedMonoSample);
            }

            bytesUnread -= bytesThisPass;
            currentCumulativeSamplePos += sample;
            Bass.ChannelSetPosition(streamHandle, songLengthBytes - bytesUnread);
        }
        
        Bass.StreamFree(streamHandle);
        
        return waveformData;
    }

    private static int CreateDecodedStream(string filePath)
    {
        return Bass.CreateStream(
            filePath, 
            Flags: 
                BassFlags.Float | 
                BassFlags.Prescan | 
                BassFlags.Decode
            );
    }
    
    public static void CreateAudioStreams()
    {
        Streams = new Dictionary<StemType, BassStream>();

        foreach (var (stem, path) in new Dictionary<StemType, string>(Chart.Metadata.StemPaths))
        {
            CreateAudioStream(stem, path);
        }
        
        StreamLink = GetLongestStream();
    }

    private static bool CreateAudioStream(StemType stemType, string stemPath)
    {
        BassStream stream;
        try
        {
            // Diagnostic: Creating a new stream is very efficient. 1 * 10^-4 ms avg
            stream = new BassStream(stemType, stemPath);
        }
        catch
        {
            Debug.LogError($"Could not load stem {stemType} from {stemPath}. Aborting load operation.");
            return false;
        }

        Streams[stemType] = stream;
        Chart.Metadata.StemPaths[stemType] = stemPath;

        stream.PlaySpeed = AudioSpeed;

        return true;
    }

    public static bool UpdateAudioStream(StemType stemType, string stemPath)
    {
        if (!CreateAudioStream(stemType, stemPath)) return false;
        
        Waveform.UpdateStemWaveformData(stemType);
        StreamLink = GetLongestStream();
        
        return true;
    }

    public static void SafeDeleteStream(StemType stem)
    {
        if (!Streams.TryGetValue(stem, out var stream)) return;

        stream.Free();
        Streams.Remove(stem);
        Waveform.RemoveStemWaveformData(stem);

        if (StreamLink == stream) StreamLink = GetLongestStream();
    }

    private static BassStream GetLongestStream()
    {
        long streamLength = 0;
        BassStream longestStream = placeholder;
        
        foreach (var stream in Streams)
        {
            var currentStreamLength = stream.Value.ByteLength;
            if (currentStreamLength <= streamLength) continue;
            
            streamLength = currentStreamLength;
            longestStream = stream.Value;
        }
        
        return longestStream;
    }

    public static void PlayAudio()
    {
        if (AudioPlaying) return;

        SetStreamPositions();
        StreamLink.Play();
        AudioPlaying = true;
    }

    public static void PauseAudio()
    {
        if (!AudioPlaying) return;

        StreamLink.Pause();
        AudioPlaying = false;
    }

    public static void StopAudio()
    {
        PauseAudio();
        SongTime.SongPositionSeconds = 0;
        AudioPosition = 0;
    }

    public static void PlayClip(float position)
    {
        SetStreamPositions(position);
        StreamLink.Play();
    }

    public static void ForceStopClip()
    {
        StreamLink.Pause();
    }

    public static void MuteStem(StemType stem) => Streams[stem].Muted = true;
    public static void UnmuteStem(StemType stem) => Streams[stem].Muted = false;

    private static HashSet<StemType> soloedStems = new();

    public static bool IsStemMuted(StemType stem) => Streams[stem].Muted;
    public static bool IsStemSoloed(StemType stem) => soloedStems.Contains(stem);
    public static bool IsAnyStemSoloed() => soloedStems.Count > 0;
    
    /// <returns>
    /// Was this the first stem to be soloed?
    /// </returns>
    public static bool SoloStem(StemType stem)
    {
        var returnFirstStreamMuted = false;
        if (soloedStems.Count == 0)
        {
            foreach (var stream in Streams.Values)
            {
                stream.Muted = true;
            }

            returnFirstStreamMuted = true;
        }
        
        UnmuteStem(stem);
        soloedStems.Add(stem);

        return returnFirstStreamMuted;
    }

    public static void UnsoloStem(StemType stem)
    {
        soloedStems.Remove(stem);

        if (soloedStems.Count > 0)
        {
            MuteStem(stem);
        }
        else
        {
            foreach (var stream in Streams.Values)
            {
                stream.Muted = false;
            }
        }
    }

    public static void SetStemVolume(StemType stem, float volume) => Streams[stem].Volume = volume;
    public static float GetStemVolume(StemType stem) => Streams[stem].Volume;
    
    private static void SetStreamPositions() => SetStreamPositions(SongTime.SongPositionSeconds + Chart.settings.Calibration);
    private static void SetStreamPositions(double position)
    {
        // Happens when no audio loaded. StreamLink exists for stability reasons, but does not exist in Streams.Values
        // as it is just a placeholder.
        if (Streams.Count == 0)
        {
            StreamLink.AudioPosition = position;
            return;
        }
        
        foreach (var stream in Streams.Values)
        {
            stream.AudioPosition = position;
        }
    }

    public enum SFX
    {
        metronome,
        clap
    }

    private static BassStream metronome;
    private static BassStream clap;
    private static BassStream placeholder;

    public static void PlayMetronomeSound() => metronome.Play();
    public static void PlayClapSound() => clap.Play();

    public static void SetSFXVolume(SFX sfx, float newVolume)
    {
        switch (sfx)
        {
            case SFX.metronome:
            {
                metronome.Volume = newVolume;
                break;
            }
            case SFX.clap:
            {
                clap.Volume = newVolume;
                break;
            }
            default:
            {
                throw new ArgumentException($"No SFX with id {sfx}");
            }
        }
    }
    
    public static float GetSFXVolume(SFX sfx)
    {
        return sfx switch
        {
            SFX.metronome => metronome.Volume,
            SFX.clap => clap.Volume,
            _ => throw new ArgumentException($"No SFX with id {sfx}")
        };
    }

    #region Encoding / Writing
    
    public static void WriteAudioFiles(
        Metadata metadata, 
        string targetDirectory, 
        AudioFormats format, 
        HashSet<StemType> includedStems,
        int bitrate
    )
    {
        Parallel.ForEach(
            metadata.StemPaths.Where(x => includedStems.Contains(x.Key)),
            x => EncodeStream(x.Key, targetDirectory, format, bitrate)
            );
    }

    private static void EncodeStream(StemType stem, string targetDirectory, AudioFormats format, int bitrate)
    {
        // Basic way that BASS encoding works is by creating an encoder attached to a stream, and then "playing"
        // that stream to "record" the data in a new file. With a decoded stream, the encoder records data when generally
        // advancing through the track and getting the data. Most code here is pretty much
        // directly lifted from BASSenc examples. 
        
        var targetFileName = Path.Combine(targetDirectory, $"{stem}.{format}");
        var handle = Bass.CreateStream(Chart.Metadata.StemPaths[stem], Flags: BassFlags.Decode);

        if (handle == 0)
        {
            Debug.LogError($"Bass error. Failed to create decode stream. Aborting encoding of {stem}. {Bass.LastError}");
            return;
        }

        int encoderHandle = format switch
        {
            AudioFormats.opus => BassEnc_Opus.Start(handle, $"--bitrate {bitrate}", EncodeFlags.AutoFree, targetFileName),
            AudioFormats.ogg => BassEnc_Ogg.Start(handle, $"-b {bitrate}", EncodeFlags.AutoFree, targetFileName),
            AudioFormats.mp3 => BassEnc_Mp3.Start(handle, $"-b {bitrate}", EncodeFlags.AutoFree, targetFileName),
            AudioFormats.wav =>
                // As far as I can tell, bitrates don't really exist in wav, since it's uncompressed.
                // The bitrate can be whatever it wants to be. Also I hate wav files. Please don't use them.
                BassEnc.EncodeStart(handle, targetFileName, EncodeFlags.AutoFree | EncodeFlags.PCM, null),
            _ => -1
        };

        if (encoderHandle <= 0)
        {
            Debug.LogError(
                $"Bass error. Failed to create encoder stream. ({encoderHandle}) " +
                $"Aborting encoding of {stem} to {targetFileName}. {Bass.LastError}");
            return;
        }
        
        var songLengthBytes = Bass.ChannelGetLength(handle);
        var bytesUnread = songLengthBytes;
        var buffer = 32768L;
        int[] buf = new int[buffer];

        while (bytesUnread > 0)
        {
            var bytesThisPass = Math.Min(buffer, bytesUnread);
            Bass.ChannelGetData(handle, buf, (int)bytesThisPass);
            bytesUnread -= bytesThisPass;
            Bass.ChannelSetPosition(handle, songLengthBytes - bytesUnread);
        }

        BassEnc.EncodeStop(encoderHandle);
        Bass.StreamFree(handle);
    }
    
    #endregion
}
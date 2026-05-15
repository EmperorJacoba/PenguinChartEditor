using System;
using ManagedBass;
using UnityEngine;

public class BassStream
{
    public StemType stem;
    private int streamHandle;

    public bool Muted
    {
        get => _mute;
        set
        {
            if (value)
            {
                Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, 0);
            }
            else
            {
                Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, _internalVolume);
            }

            _mute = value;
        }
    }
    private bool _mute;


    public float Volume
    {
        get
        {
            return Muted ? 0 : _internalVolume;
        }
        set
        {
            // value can exceed 1, is just amplification
            if (value < 0) value = 0;
            
            // Use an internal volume so that adjusting the volume of a stem in the StemVolumeEditor will still affect volume
            // when unmuted.
            _internalVolume = value;
            if (Muted) return;
         
            RefreshInternalVolume();
        }
    }
    private float _internalVolume = 1.0f;

    public void RefreshInternalVolume()
    {
        // Bass.Volume controls the OS volume level and lowk sucks
        // This is an alternative to that, where master volume is just a volume scalar as opposed to actually changing
        // the system volume. 
        
        Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, _internalVolume * AudioManager.MasterVolume);
    }

    // ChannelAttribute.Speed seems to be another option - perhaps the other form of speed? freq vs. sample cut
    public float PlaySpeed
    {
        get
        {
            // Don't want to call BASS when there is literally no reason to
            return _spe;
        }
        set
        {
            if (value < 0) value = 0;
            Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Frequency,
                Bass.ChannelGetInfo(streamHandle).Frequency * value);
            _spe = value;
        }
    }
    private float _spe;

    public double AudioPosition
    {
        get => Bass.ChannelBytes2Seconds(streamHandle, Bass.ChannelGetPosition(streamHandle));
        set
        {
            Bass.ChannelSetPosition(streamHandle, Bass.ChannelSeconds2Bytes(streamHandle, value));
        }
    }

    public BassStream(string filePath)
    {
        stem = (StemType)(-1);

        streamHandle = CreatePlayingStream(filePath);
        
        if (streamHandle == 0)
        {
            throw new ArgumentException($"BASS error when creating sfx stream. {Bass.LastError}");
        }

        Volume = 1f;
        Muted = false;
    }
    
    public BassStream(StemType stem, string filePath)
    {
        this.stem = stem;
        streamHandle = CreatePlayingStream(filePath);

        if (streamHandle == 0)
        {
            throw new ArgumentException($"BASS error when creating {stem} stream. {Bass.LastError}");
        }

        Volume = 1f;
        Muted = false;
    }

    private static int CreatePlayingStream(string filePath)
    {
        return Bass.CreateStream
        (
            filePath,
            Flags: BassFlags.Default | BassFlags.Prescan
        );
    }
    
    public void Free()
    {
        Bass.StreamFree(streamHandle);
    }

    public void Play()
    {
        if (!Bass.ChannelPlay(streamHandle))
        {
            Debug.LogWarning($"There was an error playing a stream handle. Bass Error {Bass.LastError}");
        }
    }

    public void Pause()
    {
        if (!Bass.ChannelPause(streamHandle))
        {
            Debug.LogWarning($"There was an error pausing a stream handle. Bass Error {Bass.LastError}");
        }
    }

    public long ByteLength => Bass.ChannelGetLength(streamHandle);
    public double TimeLength => Bass.ChannelBytes2Seconds(streamHandle, ByteLength);

    public void LinkTo(BassStream targetStream)
    {
        if (streamHandle == targetStream.streamHandle) return;
        if (!Bass.ChannelSetLink(targetStream.streamHandle, streamHandle))
        {
            Debug.LogError($"Bass error linking stream {stem} to {targetStream.stem}. {Bass.LastError}");
        }
    }
}
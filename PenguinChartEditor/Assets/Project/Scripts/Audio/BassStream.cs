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
            return (float)Bass.ChannelGetAttribute(streamHandle, ChannelAttribute.Volume);
        }
        set
        {
            // Use an internal volume so that adjusting the volume of a stem in the StemVolumeEditor will still affect volume
            // when unmuted.
            _internalVolume = value;
            if (Muted) return;
            if (value > 1) value = 1;
            if (value < 0) value = 0;

            Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, value);
        }
    }
    private float _internalVolume;

    // ChannelAttribute.Speed seems to be another option - perhaps the other form of speed? freq vs. sample cut
    public float PlaySpeed
    {
        get
        {
            return (float)Bass.ChannelGetAttribute(streamHandle, ChannelAttribute.Frequency);
        }
        set
        {
            if (value < 0) value = 0;
            Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Frequency,
                Bass.ChannelGetInfo(streamHandle).Frequency * value);
        }
    }

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

       // Volume = 1f;
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
        return 
            Bass.CreateStream
            (
                filePath,
                Flags: BassFlags.Default | BassFlags.Prescan
            );
    }
    
    public void Free()
    {
        Bass.StreamFree(streamHandle);
    }

    public void Play() => Bass.ChannelPlay(streamHandle);
    public void Pause() => Bass.ChannelPause(streamHandle);

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
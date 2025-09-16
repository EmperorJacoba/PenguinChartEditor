using System;
using UnityEngine;

public class Chart : MonoBehaviour
{
    static Metadata Metadata { get; set; } = new();
    static Chart instance;

    /// <summary>
    /// Number of ticks per quarter note (VERY IMPORTANT FOR SONG RENDERING)
    /// </summary>
    public static int Resolution
    {
        get
        {
            if (_chartResolution == 0)
            {
                _chartResolution = ChartParser.loadedChartResolution; // this will not work when reloading files, fix later
            }
            return _chartResolution;
        }
        set
        {
            if (value == 0) throw new ArgumentException("Resolution cannot be zero!");
            _chartResolution = value;
        }
    }
    private static int _chartResolution = 0;

    public static string FolderPath { get; private set; } = "";
    public static string ChartPath { get; private set; } = "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/Perpetual Change.chart";
    public static string IniPath { get; private set; } = "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/song.ini";

    public void SaveFile()
    {

    }

    public void LoadFile()
    {

    }

    void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
        }

        instance = this;
        DontDestroyOnLoad(instance);

        Metadata.TempSetUpStemDict();
    }
}
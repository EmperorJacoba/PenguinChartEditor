using System;
using System.IO;
using System.Collections.Generic;


public class Metadata
{
    /// <summary>
    /// Stores the directory of the album cover selected by the user.
    /// </summary>
    public string ImagePath { get; set; } // set with SetAlbumCover()

    /// <summary>
    /// Stores the directory of the background set by the user.
    /// </summary>
    public string Background { get; set;}

    /// <summary>
    /// Stores valid song metadata fields.
    /// </summary>
    public enum MetadataType
    {
        name,
        artist,
        album,
        genre,
        year,
        charter,
        song_length,
        preview_start_time,
        icon,
        loading_phrase,
        album_track,
        playlist_track,
        video_start_time,
    }

    public Dictionary<MetadataType, string> SongInfo = new();

    // All of these values store difficulties in a value from 0-6, although values higher than 6 are allowed for some niche CH uses.
    // Set up these values in the CHART tab - no sense setting them up when you don't have the tracks charted yet!
    /// <summary>
    /// Stores valid instrument difficulties.
    /// </summary>
    public enum InstrumentDifficultyType
    {
        diff_band,
        diff_guitar,
        diff_guitar_coop,
        diff_rhythm,
        diff_bass,
        diff_drums,
        diff_drums_real,
        diff_elite_drums,
        diff_keys,
        diff_keys_real,
        diff_ghl,
        diff_bass_ghl,
        diff_vocals,
        diff_vocals_harm
    }

    public Dictionary<InstrumentDifficultyType, int> Difficulties = new();

    /// <summary>
    /// Stores valid types of audio stems.
    /// </summary>
    public enum StemType
    {
        // 0 is reserved for none
        song = 1,
        guitar = 2,
        bass = 3,
        rhythm = 4,
        keys = 5,
        vocals = 6,
        vocals_1 = 7,
        vocals_2 = 8,
        drums = 9,
        drums_1 = 10,
        drums_2 = 11,
        drums_3 = 12,
        drums_4 = 13,
    }

    public Dictionary<StemType, string> StemPaths = new();

    // test paths to make this easier
    static string[] stems = new string[6] {
        "", // song
        "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/Guitar.opus", // guitar
        "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/Bass.opus", // bass
        "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/Keys.opus", // keys
        "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/Vocals.opus", // vocals
        "C:/_PCE_files/TestAudioFiles/Yes - Perpetual Change/Drums.opus"  // drums
    };

    public void TempSetUpStemDict()
    {
        //Stems[StemType.song] = stems[0];
        StemPaths[StemType.guitar] = stems[1];
        StemPaths[StemType.bass] = stems[2];
        StemPaths[StemType.keys] = stems[3];
        StemPaths[StemType.vocals] = stems[4];
        StemPaths[StemType.drums] = stems[5];
    }
}
# Welcome to the future of charting!

Penguin Chart Editor is a work-in-progress chart editing software for Guitar Hero/Rock Band-style rhythm games, that will support five-fret and six-fret guitar-based instruments, 
four-lane and eight-lane (Elite) drums, and most importantly, **Vocals/Harmonies gamemodes**. Pro Keys is a reach goal, pro guitar/bass & 5L drums are not planned but are considered.

Unlike other chart editors, Penguin separates the stages of chart editing into distinctive "tabs," each structured to best achieve a certain task.  
This method of chart editing is inspired by Steinberg's Dorico, a program used for creating sheet music.

## An important note about Penguin

### **Please do not submit issues or pull requests at this time. If you have any feature requests, questions, feedback, or want to contribute, please join the [discord server](https://discord.gg/z2UTt2p6uM) and contact Emperor.**

Penguin is **still in development**, and no built version of Penguin exists yet. I hope to have an alpha version of Penguin released by **March-April 2026**. 
**Please star and watch this repository to get notifications when I (eventually) release builds!**

The first version (version 0.1.0-alpha) of Penguin will include the following:

- **Song setup**/metadata tab (enter in song metadata, audio stems, etc)
- **Tempo Map** tab (set beatline positions and time signature changes)
- **Chart** tab with support for five-fret instruments (guitar, bass, coop guitar, rhythm, keys)
- **Starpower/Sections** tab (set starpower for instruments, lay down practice sections)
- **Export** tab (create chart directory/file/folder as .zip or .chart)

Files will be saved as .penguin, a file format that mirrors the data structure of Penguin.

In following versions (roughly in this order - no promises), more features will be added:

- Undo/Redo
- Customizable keybinds/settings - including Lefty Flip
- .mid support: importing and exporting
- ***Vocal charting***
- Pro drum charting
- Bookmarks (local editor sections)
- Elite drum charting
- Six-fret (GHL) charting
- RB3/YARG venue, lighting, and character animation editing tab
- Linux support

Plus other major & minor features not listed. Planned features are usually outlined as an issue in the [issues tab](https://github.com/EmperorJacoba/PenguinChartEditor/issues).

Penguin Chart Editor is being developed with [Unity, version 6000.0.60f1.](https://unity.com/releases/editor/whats-new/6000.0.60f1)

# Images
<figure>
  <img width="1959" height="1122" alt="Five fret charting - Previewer" src="https://github.com/user-attachments/assets/051cd531-d516-44a6-bb5f-55810a75f199" />
  <figcaption>You can place notes with a predefined sustain length.</figcaption>
</figure>

--

<figure>
  <img width="1961" height="1122" alt="Five fret charting - Tap note and selection" src="https://github.com/user-attachments/assets/7f6919c5-1d6e-49af-b59c-21ac282574a5" />
  <figcaption>Selection options/modifications have many common modifications you would want to make to a selection, including a custom sustain length.</figcaption>
</figure>

--

<figure>
  <img width="1948" height="1104" alt="Five fret charting - mixer and chords" src="https://github.com/user-attachments/assets/3afa48cb-f62a-4cfa-9c32-7813175f6b9a" />
  <figcaption>Penguin comes with an easily accessible mixer with standard mute and solo functionality.</figcaption>
</figure>

--

<figure>
  <img width="1920" height="1080" alt="Starpower tab" src="https://github.com/user-attachments/assets/36151e5c-23ee-4ef4-baa6-41c1b1d2e281" />
  <figcaption>This is the sections/starpower tab. You can display any number of the instruments you've charted next to each other for easy creation of starpower, RB unison phrases, BREs, sections, and drum fills.</figcaption>
</figure>

--

<figure>
  <img width="1940" height="1098" alt="Five fret charting - solo display" src="https://github.com/user-attachments/assets/8101c699-a953-41da-a2df-5b98f99e1eed" />
  <figcaption>This is what a solo section looks like in Penguin. It is designed to mimic the appearance of a solo in YARG/RB3, and automatically accounts for end events, meaning you'll never accidentally leave a solo open forever again. Additionally, the third yellow note is forced, which is easily identifiable by the light blue base of the note. Display options, like hyperspeed, amplitude (the appearance of the waveform), play speed, and highway length, are infinitely customizable (so long as your computer can handle it).</figcaption>
</figure>

--

<figure>
  <img width="1970" height="1138" alt="Tempo Map" src="https://github.com/user-attachments/assets/6b54700b-ee5e-4b79-ba62-2c8d60b636d6" />
  <figcaption>The tempo map tab is structured as a pure 2D, top-down view of a track for the highest level of precision when tempo mapping. This is unlike Moonscraper, where the distortion of the waveform by the angled track and large, blocky, event indicators covering the waveform makes it hard to tempo map. You can adjust BPM changes with the standard control+click+drag event you're familiar with in Moonscraper, or adjust time signature events and BPM events directly through the label (which can be edited with a simple double-click). </figcaption>
</figure>


# Attributions

This program uses [BASS](https://www.un4seen.com/bass.html) and [BASS.NET](https://www.radio42.com/bass/) for audio functionality. These pieces of software are proprietary. Penguin is licensed as freeware for both of these pieces of software.

Penguin also uses [UnityStandaloneFileBrowser](https://github.com/gkngkc/UnityStandaloneFileBrowser) for file selection.

Instrument icons come from [YARG](https://github.com/YARC-Official/YARG/blob/master/Assets/Art/Menu/Common/InstrumentIcons.png). 

[Metronome](https://thenounproject.com/browse/icons/term/metronome/). Settings icon designed by Freepik.

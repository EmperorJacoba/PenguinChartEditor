# Penguin Chart Editor
### a new way to chart Guitar Hero/Rock Band/Clone Hero/YARG custom songs

**[Join the discord!](https://discord.gg/3QYRbTGzS5)**

Penguin Chart Editor is a new chart editing software designed for speed and ease of use. 
Unlike other chart editors, Penguin separates the stages of chart editing into distinctive "tabs," each structured and streamlined to best achieve certain charting tasks. Each tab is designed in a simple, accessible, and user-friendly way.
This method of chart editing is inspired by Steinberg's Dorico, a program used for creating sheet music.

**To download and use Penguin, please see the releases tab in the right sidebar.**

*Note: custom keybinds are not yet implemented. In the meantime, please [see built-in keybinds](https://github.com/EmperorJacoba/PenguinChartEditor/blob/main/Keybinds.md)*

Penguin is currently in alpha, meaning that bugs should be expected and the current state/feature set/appearance
of the program is not finalized. Penguin currently only has a working build for Windows. Linux/MacOS are in the works.

Penguin Chart Editor is being developed with [Unity, version 6000.0.60f1.](https://unity.com/releases/editor/whats-new/6000.0.60f1)

# FAQ

## What can I currently do with Penguin?

Penguin currently primarily supports five-fret instrument charting, along with a tempo mapping and starpower tab. Essentially,
the tools required to make a full five-fret chart from start to finish.

## What's coming next?

I plan to expand on the available instruments, including:

- Four-lane (pro) drums
- Vocals
- Elite (eight-lane) drums
- Six-lane (GHL) instruments

as well as new QoL features, such as:

- chart splicing in-editor (combining/splitting charts)
- audio editing in-editor (add/remove leading/lagging silence)
- chart blueprints (reusable chart patterns)
- chart bookmarks (to save notable editing/revision locations)

and support for file formats such as:

- .mid
- .sng
- .rb2CON
- .rb3CON

> note: PenguinChartEditor saves data as .penguin. Exporting/reading .chart files is already supported.

## How do I download Penguin?

See the "releases" tab in the right sidebar and follow download instructions for the latest release.

## How do I give feedback/report bugs?

If you encounter a bug in editor, please report it using the "Issues" tab in the top ribbon in this repository. 
Please provide the steps you took leading up to the issue with screenshots/videos of the unexpected behavior as well as your log file and a description of the issue. 
I will do my best to contact you promptly and fix the bug as soon as possible. 

Instructions on how to find the log file (windows):
1. Use `Win+R` to open the "run" window and type `%APPDATA%`, and then hit "OK".
2. Go back one folder to the `{username}\AppData` folder. (not `{username}\AppData\Roaming`)
3. Click on the `LocalLow` folder, then `EmperorJacoba` folder, then `PenguinChartEditor`.
4. Upload `player.log` to your issue.

To give feedback or feature requests, please contact me via the [discord server](https://discord.gg/3QYRbTGzS5) 
and/or submit issues marked as feature requests. 
I appreciate feature requests and feedback! I want Penguin Chart Editor to be the best it can be and satisfy as many charting needs as possible.

## What do I do if Penguin crashes?

If you had unsaved changes, find the latest autosave file to recover your changes (Penguin autosaves every 10 seconds).

1. Use `Win+R` to open the "run" window and type `%APPDATA%`, and then hit "OK".
2. Go back one folder to the `{username}\AppData` folder. (not `{username}\AppData\Roaming`)
3. Click on the `LocalLow` folder, then `EmperorJacoba` folder, then `PenguinChartEditor`.
4. Locate the latest autosave (sort by "date modified").

Please take screenshots of the error notification (if one appears) and report the issue using the instructions above.

## How do I contribute?

As Penguin is open source, you are free to fork and modify (and submit pull requests to) Penguin at your own discretion. 
However, if you are creating very large features/bug fixes, please contact Emperor to discuss changes so that they can be cleanly implemented.

If you need any assistance/guidance navigating the codebase, please contact Emperor. I will create proper documentation of the codebase in the future. 

## When will the full release be?

The full release will be when all targeted features are implemented stably. 
I do not have an exact timeframe for this due to other commitments in my life 
(I work on Penguin whenever I get the chance between classes, work, etc.)

# Screenshots

<img width="1920" height="1041" alt="PenguinChartEditor_pU9z5vAQtk" src="https://github.com/user-attachments/assets/742cc434-db37-45ba-ba76-e5238f1ade0e" />
<img width="1920" height="1041" alt="PenguinChartEditor_FKzFk8gLKp" src="https://github.com/user-attachments/assets/7bbf22fd-9b33-4810-8fb1-e131c079b057" />
<img width="1920" height="1041" alt="PenguinChartEditor_BgFu4gnCZU" src="https://github.com/user-attachments/assets/1b1b1265-9e54-410c-b052-b5095432ffc4" />
<img width="1920" height="1041" alt="PenguinChartEditor_EPrk2TASU0" src="https://github.com/user-attachments/assets/1dfacbbc-4cfc-48aa-90d7-1f4b1af8ff98" />
<img width="1920" height="1041" alt="PenguinChartEditor_G45KWkGlyU" src="https://github.com/user-attachments/assets/b984d88d-81d2-41b7-8d0b-47a58062b970" />

# Limitations/Known errors

- Loading extremely large charts (estimated >10,000 notes) have not been rigourously tested and will be slow. 

# Attributions

This program uses [BASS](https://www.un4seen.com/bass.html) for audio functionality, which is proprietary, licensed software. Penguin Chart Editor is licensed under freeware. Please obtain a license of your own if you are repackaging this code.

Penguin also uses [UnityStandaloneFileBrowser](https://github.com/gkngkc/UnityStandaloneFileBrowser) for file selection.

Instrument icons come from [YARG](https://github.com/YARC-Official/YARG/blob/master/Assets/Art/Menu/Common/InstrumentIcons.png). 

[Metronome](https://thenounproject.com/browse/icons/term/metronome/). Settings icon designed by Freepik.

(Load icon) Folder by Landan Lloyd from <a href="https://thenounproject.com/browse/icons/term/folder/" target="_blank" title="Folder Icons">Noun Project</a> (CC BY 3.0)

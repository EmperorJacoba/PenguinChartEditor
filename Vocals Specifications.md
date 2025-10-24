# Penguin Chart Editor Vocals Specifications

### Purpose of this document

This document defines how vocal editing should be handled within Penguin itself (the user interface, methods of displaying data, etc), and also how Penguin should save data to a .chart file. Penguin should always export charts with vocal data as .mid. 

> Note: .sng should also be an export option. Follow this same rule if .sng does not support .mid file data. If they do, export as .mid and then use converter to convert to .sng.
RB2CON and RB3CON should also eventually be export options. The most practical way of doing this is likely via exporting as .mid and then plugging into [Onyx](https://github.com/mtolly/onyx). Make sure to attribute it if used! 

Penguin should allow exporting charts as .chart with vocal data, but **strongly** warn against doing so. Opening a .chart file in Moonscraper and saving will delete all [Vox] data. However, a .chart file saved from Penguin with vocal data should be able to open up save data perfectly. Possibly save as \<Chart Name\>.penguin but use .chart encoding? 

> Note: Penguin will also need to save small binary metadata files for QoL features like opening back up at the same timestamp. This should be a hidden file called data.penguin. If chart data is saved as \<Chart Name\>.penguin, save data.penguin as a file called .pcedata or .penguindata.

### Terminology

A "lyric event" refers to a lyric event, formatted as `<Tick> = E "lyric <Text>"` as seen in `[Events]`.

A "vocal event" or "vocal note" refers to the pitch data in `[Vox]` or `[Vox<#>]` marked-up as `<Tick> = N <Pitch> <Sustain>`. Every lyric event must correspond with a vocal note on export, but can be fluid in saving working/editor versions. If there are no vocal events present, ignore the prior rule (the charter just wants lyrics, no pitched support)

# Basic Format
```
[Vox]
{
  <Tick> = N <Type> <Sustain>
  <Tick> = N <Type> <Sustain>
  <Tick> = N <Type> <Sustain>
}
```

`<Type>` = midi note, range 36-84. This is for simplicity and parallel workings with .mid files.

> Note: with unpitched notes, \<Type\> should be `0`.
> This shouldn't matter that much, as the editor should realize the note is unpitched, but this should be used in case the `#` is lost somewhere down the line. Also to signal that there actually should be a lyric event rendered on the scene.

Example usage:
```
[Vox]
{
  480 = N 74 4800
}
```
Meaning: at tick 480, there is a vocal event at note D5, with a sustain of 4800 ticks (Resolution = 480)

This should match up with a lyric event in `[Events]`. 
Example usage with prior data:
```
[Events]
{
  480 = E "lyric Ham"
}
```
Meaning: at tick 480, the vocal event data in `[Vox]` with the same tick has a lyric of "Ham".

This match-up should be used for backwards-compatibility with Moonscraper, CH, etc., as much as I would like for `[Vox]` to also have lyrics in it.

`[Vox1]`, `[Vox2]`, `[Vox3]` are harmonies tracks. Since these lyrics aren't processed in the same way as main vocals in Moonscraper for .mid files (they are not shown or considered), there are no backwards compatibility issues with lyrics being shown in other programs. Therefore, text events can and should appear alongside pitched notes for `[Vox1]`, `[Vox2]`, and `[Vox3]`.

Example:
```
[Vox1]
{
  3200 = N 45 640
  3200 = E "lyric Yeah!"
}
```

# User Interface

Here's a quick sketch I drew of what the UI should look like: 

<img width="1622" height="1347" alt="A sketch of how the lyrics UI should look." src="https://github.com/user-attachments/assets/9bc46a96-3332-4e22-a5e4-0f33f5877769" />

Components:
- Lyric event bar
- Pitched vocal events section (body)
- Options panel/TimeEditor
- Pitch grid
- Spectrogram/waveform in body

Things to note:
- Lyrics are separate entities from pitched.
- Spectrogram should be built-in, and fitted to the pitches on the left. If a singer sings a C#3 pitch, then the spectrogram should show a heat intensity at the y-level of the C#3 grid position. Spectrogram should also be able to scroll past the midi pitch limits, but warn that any notes put outside the boundaries will not be exported (but will be saved). This allows users to see other parts of the spectrogram if they need to.
- Background can also show static waveform when toggled.
- `+` is not shown as the lyric for the second part of the slide note. See below. 

## Harmonies

Harmonies should be a separate charting window from standard vocals (`PART VOCALS` is separate from `HARM1`, `HARM2`, and `HARM3` in .mid). Allow users to duplicate Vox to Harmonies, and vice versa.

Notes in `[Vox1]`, `[Vox2]`, and `[Vox3]` should show up alongside each other as pitched vocal events, but as different colors. The shades of color should be the same as in Rock Band 3. For overlapping events, superimpose blue over red over yellow (shrink the size of the smaller lyric event and show it on top of the harmony).

Light Blue= `[Vox1]`

Red = `[Vox2]`

Yellow = `[Vox3]`

Allow users to select which track they are editing with a keybind/button. The previewer object's ghost should match the new color it is editing. Deleting notes should only apply to the user's currently selected editing vocal track, especially for superimposed notes. Users should also be able to change which track an event belongs to, and duplicate them across tracks.

Lyric track at top of screen should change to match whichever harmonies track it is editing. Example: editing blue (main) vocals shows only main lyrics at top of screen, editing red shows only red (harm2) lyrics.

# Lyric Accessories

These lyric accessories (symbols in the middle of or at end the end of lyrics) should not be shown in editor (mostly).

Make sure to add a setting to allow experienced vocal charters implement these manually with lyric events. Any abstractions (like not showing `+` on the end of a slide note, instead just showing the slide itself) should be opt-out. When opting-out, ALL lyric events are shown, including the accessories. Same methods of implementing them apply, but with the extra option of allowing the user to type the accessory manually, which Penguin should display as if the user used the abstracted way instead. 

This also means that in the VoxData struct, VoxData must have a string for the raw lyric event interpretation, that when set, should change the properties the struct has. See VoxData section.

Table from [TheNathannator](https://github.com/TheNathannator/GuitarGame_ChartFormats/blob/main/doc/FileFormats/.mid/Standard/Vocals.md?plain=1)

### Hyphen `-`
Join this syllable with the next syllable

Example: 

`E "lyric Hel-" `

`E "lyric lo"`

= Hello (displayed as Hel- lo over corresponding pitches in editor and in game)

Show this as written in editor. Piano roll notes should not connect. Most charters are used to this and will be fine with seeing it, and will probably be able to work better by seeing which lyrics connect into words.

### Plus `+`
This vocal event connects pitch-wise with the last vocal event (a vocal slide)
Example:
```
[Events]
{
  10000 = E "lyric rats!"
  10100 = E "lyric +"
}

[Vox]
{
  10000 = N 70 50
  10100 = N 66 500
}
```

This would display as a note of A#, beginning at tick 10000, sustained for 50 ticks

Then, a linear connection between A# and F# over 50 ticks (10100 - 10000 - 50 = 50)

And finally, an F# note beginning at tick 10100, sustained for 500 ticks. 

The F# note would have no lyric above it. New notes placed should have no lyrics above them, and should not be required by default. The user can add lyrics when they are ready (see Lyric Events).

Vocal editor should treat each block individually. To create this, the user would:

1. Create A# note at 10000 with a sustain of 50 ticks
2. Create F# note at 10100 with a sustain of 500 ticks
3. Either a) select both, and then press a button/keybind to connect them as a slide
or b) right-click drag the end of A# to the beginning of F#

### Equals `=`
This lyric ends with a literal hyphen (used for non-pitched viewing like in CH)

Same implementation as hyphen. This should not need an abstraction, as imo any abstraction of this will cause confusion with the regular hyphen (for example, if both show up as hyphens, but one is colored differently)

### Pound/Hash `#`
> Note: `*` has also been known to signal unpitched events. Parse it as such in incoming files, but always export with `#`.

This lyric does not have a pitch.

Example:
```
[Events]
{
  480 = E "lyric Mail-#"
  960 = E "lyric man#"
}

[Vox]
{
  480 = N 0 240
  960 = N 0 4800
}
```
The lyrics "Mail-" and "man" should show up as a lyric event covering the entire length and width of the lyric section (basically a big, blue, slightly translucent box covering the track for the length of the event).

For an example, see what unpitched notes look like on RB vocal tracks.
  
The lyrics themselves should NOT show the hash. Only put the hash in the lyric event in the save data itself.

Users should be able to create unpitched notes via:

- clicking and dragging to create a pitched lyric event normally, and then using a button/keybind to convert it to unpitched
- dragging directly over the lyric bar, or slightly below it
- c) clicking and dragging the length of the note, and a user-specified width on the track (basically forming a rectangle)

Select the unpitched note by clicking anywhere in the unpitched event box.

Users should be able to convert an unpitched note back to a pitched note by selecting it, and then using a button/keybind to switch it to a pitched note. A small prompt should appear for the user to enter a midi pitch or a letter pitch (e.g. A#4), where the lyric event should appear. 

### Caret `^`
This lyric does not have a pitch, but the game should treat scoring it leniently. 
Usually used for short notes, or syllables without sharp attacks.

Example:
```
[Events]
{
  480 = E "lyric Mail-^"
  960 = E "lyric man^"
}

[Vox]
{
  480 = N 0 240
  960 = N 0 4800
}
```
This should be intepreted the same way as `#`. Allow user to toggle unpitched as lenient or regular. Differentiate with colors. Regular is blue for same RB styling, caret is green.

### Percent `%`

At the end of a phrase, recalculate the range of notes that is shown. The notes before `%` are shown in one range, the notes after in another.

Example:
```
[Events]
{
  50500 = E "phrase_start" // discussed later
  60000 = E "lyric cake"
  60500 = E "lyric can-"
  61000 = E "lyric non%"
  61500 = E "phrase_end" 
  62000 = E "phrase_start"
  62500 = E "lyric "por-"
}

[Vox]
{
  60000 = N 40 250
  60500 = N 42 250
  61000 = N 40 250
  62500 = N 72 1000
}
```
Since midi notes 40-42 are very far away from 72, RB would ordinarily calculate one range for all vocal notes that is extremely long, which would appear very squished in gameplay. Players would not be able to match pitches very easily. 

The `%` fixes that. Ticks 0 - 61000 appear in a range that perfectly fits 40-42, and ticks 62500 - end appear in a good range for 72. 

Penguin should detect when to do this automatically. [C3 says](http://docs.c3universe.com/rbndocs/index.php?title=Vocal_Authoring) that a normal range is 2 - 2.5 octaves. (24-30 pitches). This is a problem for songs that have different sections in wildly different ranges. Penguin should calculate the song's range, and pick points where it would make sense to put a `%` to properly range shift.

This applies to the static vocals mode only. The same functionality is achieved for the default scrolling mode with the `[range_shift]` or `[range_shift <time>]` text event. Penguin should add a `%` and a `<Tick> = E "[range_shift]"` event in its calculations. 
> In midi, this is apparently C -2?? Or a text event?? this is unclear [(source)](http://docs.c3universe.com/rbndocs/index.php?title=Harmony_Authoring) - Nathannator also shows this in his midi table.

In the advanced (opt-in) mode (which would show all accessories), allow users to select where to use `%`. For `[range_shift]`, opt-in mode should add a seperate section, at the bottom of the scrollable window with the vocal events in it, where users can put their own `[range_shift]` events. 

### Subsection `§`
[Explained here](http://docs.c3universe.com/rbndocs/index.php?title=Spanish_Syllables)

This lyric has two syllables within it, but are being sung as one. 
If a charter needs this, they should put in the symbol themselves. Penguin won't need to show anything extra in the editor. 

### Dollar `$`
Harmonies only. Add at the end of a lyric to show that the lyric attached should be hidden.

Implement with a keybind/checkbox for a harmonies note to signal that it should be hidden. 

### Slash `/`

Splits a phrase for scrolling in static vocals mode. This is not often used, so charters should use the raw character to indicate it. 

### Underscore `_`

Stands in for a space. This is not necessary for modern programs afaik, so charters should use the raw character.

### Angle Brackers `<>`

Either stands in for asterisks or a text formatting tag. 

Allow users to type in angle brackets for an action (\*action\*), but formatting tags should be an opt-in option. The option should add a left options panel tab where you can mess with the formatting of lyrics (example: add italics, bold, underline, etc.)

# Placing Lyric Events

Lyric Event placement and vocal event placement should be separate. Lyric events can be created in two ways: a) via rhythm-based matching - like moonscraper lyric editor & set time with enter as the track plays or b) manually via the top bar

Unlinked lyric events (ones that do not correspond with a pitched or unpitched vocal event) should appear as white along the top bar. Linked events should appear as the color of the current edited track (e.g. blue, red, yellow), in-line with the vocal event it is linked to. Moving either event when linked should move the other component, until unlinked by the user.

Linked events should have the **exact** same tick as the vocal event it is linked to. When reading files, assume lyric events that happen on the same tick as a vocal event are linked. If there is an exception to this when saving, store this in the .penguindata metadata file.

Linking events should prioritize pitched vocal note position over the lyric's position.

Lyric events should also be left justified against the tick at which it occurs on (gray line at exact position, followed by the lyric). It is okay if they overlap. Overlap can be solved by zooming further in.

### Rhythm-based
This should allow user to type in hyphenated lyrics of a song and then let them set timestamps for each vocal event by tapping along to the vocals. See Moonscraper lyric editor. 

The user should be able to do this for an entire song, or just a selected section (time-based input). Set a "begin" marker and an "end" marker for when to allow syncing.

When used, and pitched vocal notes already exist, lyrics should be matched up to the next closest vocal event within a user-specified tick-range. This allows a charter to chart the pitches for half the chart, set lyric events, and then set the rest later, if they choose to do so, without overwriting or incorrectly mapping lyrics to faraway events.

When pitched vocal notes do not exist, lyrics should be matched to the closest tick (on a specified grid, like 1/32 or 1/64) they were tapped at. Then, the user can either spawn a corresponding vocal event with the same tick with a button/keybind and specify its pitch in a popup, or manually create a vocal event and then link them.  

### Manual
The user can create lyric events by clicking the top bar and inputting a string (invisible input field? show output, but not surrounding box). There should be a previewer like for any other event type, that follows the user's current specified tick grid (Division). These are not immediately linked to a vocal event; they are free-floating. 

Then, after the user creates vocal notes, the user should be able to select the lyric event and use a keybind/button to link it to the nearest vocal event, or select a lyric event and a vocal event and use a keybind/button to link them. Linking the events should put the lyric event in-line with the vocal event. 

# Placing Vocal Events

The user should be able to click to create a vocal event on a pitch grid. A standard click should produce an event of tick length { resolution * 4 / division }. A click and horizontal drag should produce an event at the specified position in the event grid with length specified by the user's drag delta from the starting position. Follow user's tick grid for sustain length, unless snapping is disabled by user, in which case, translate delta to raw tick length. 

To edit a sustain, right click and drag on the tail end of a vocal event. 

To move a note, click and drag on the note itself.

To make a vocal event unpitched, click and drag a square of length { sustain amount } x width { user-specified grid distance }. User can also make a pitched event unpitched and vice versa with a keybind/button. 

> Pitched => unpitched => pitched should remember old pitch when going back to pitched. Unpitched => pitched should prompt for a pitch. 

# `[phrase_start]`, `[phrase_end]`

These events appear as such:
```
[Events]
{
  0 = E "phrase_start"
  (...lyrics...)
  5300 = E "phrase_end"
}
```
Both of these should appear as vertical lines cutting through both the lyric and vocal event editors. `[phrase_start]` should be a slightly-translucent green line, and `[phrase_end]` should be a slightly-translucent red line. These should stay the same (thin) width as the editor zooms.

To place `[phrase_start]` and `[phrase_end]` events, user should be able to use a button/keybind to switch to phrase editing mode, and place phrase bars. Editor should detect whether the current bar is a phrase start or phrase end, unless manually toggled by the user (which should prompt a warning symbol above the phrase marker). 

Phrase markers can be placed on the start and ends of "note tubes" [(source)](http://docs.c3universe.com/rbndocs/index.php?title=Vocal_Authoring), but Penguin should export them as having a 1/128 gap between it and the note it sits on top of (before for start, after for end). Saving the data normally should save it as-is (no forced gap). 

# Pitch Preview

User should be able to specify via a button/keybind to preview pitches. User should also be able to specify which harmonies tracks to include/exclude in the preview. 

Penguin should take vocal data and translate it to playing a pitch in real time as the song is being played in the editor. Use a midi or instrument plugin to do so. 

# Percussion

Percussion events should be added in the lyric event bar. They should show up as circles, like they do in RB, and should be a toggle-able mode with a keybind/button. 

> Another possible (better) option: show circles as a tambourine, cowbell, or clap to show which animation RB3 will use. Store generated animation flags (ex. [tambourine_start]) in [Events]. If this approach is used, make sure the character/venue/animations tab allows editing this flag.

These should be notated in the [Vox} track as such: 
```
[Vox]
{
  320 = S 96 0
  640 = S 97 0
}
```
`S 96` = displayed percussion

`S 97` = hidden percussion (only play sample)

> Why 96/97? These are the midi note identifiers for percussion on tracks.

[C3 says you need to set percussion sample in MAGMA (under Percussion Sections)](http://docs.c3universe.com/rbndocs/index.php?title=Vocal_Authoring). How does YARG deal with this? Does YARG deal with this?

If PCE ever deals with exporting to encrypted RB3CON (possibly by plugging into [Onyx](https://github.com/mtolly/onyx)), PCE should collect a percussion.opus file in the chart parent directory and export the percussion sample as that. Otherwise, export with the animation flags and nothing more. 

# Star Power

`[Vox]` and `[Vox1]`, `[Vox2]`, `[Vox3]` have separate star power. Star power for harmonies appears in `[Vox1]`. Star power should appear like it does in other instruments. Example:
```
[Vox]
{
  855 = S 2 2400 // star power for 2400 ticks
  860 = N 55 900
}
```
# VoxData

This is a rough draft of what VoxData should look like to accommodate advanced and abstracted modes of vocal editing specified above. Raw strings should be processed for advanced, modifiers for abstracted.
```
lyricString = "Mail#"
voxData = new VoxData(lyricString)
```
OR
```
var lyricString = "Mail"
var modifiers = new List<VoxFlag>() {VoxFlag.unpitched}
voxData = new VoxData(lyricString, modifiers)
```
```
struct VoxData
{
  public VoxData(string lyric)
  {
    // interpret lyric string here (e.g. Mail# inteprets the unpitched flag)
  }
  
  public VoxData(string lyric, List<VoxFlag> flags)
  {
    // interpret lyric string and apply flags
  }
}
```

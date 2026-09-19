<p align="center"><img src="art/rst_icon_1024.png" width="128" alt="RST"></p>

# RS3 Timers

A small Windows desktop app for RuneScape 3 timers. Farming patches and Player-owned farm animals count down and chime when ready. Player-owned farm buyers and the weekly and monthly resets count down on the game clock, with a checklist of weekly and monthly activities that clears itself at each reset.

Built for one player's own use and shared as is. No login, no game hooks, no data leaves your PC.

## Download and run

1. Grab `RS3Tracker.exe` from the [Releases](../../releases) page.
2. Put it in a folder of its own. It saves your timers in a file next to itself.
3. Double click it.

The first launch shows the Windows SmartScreen warning ("Windows protected your PC"). That is what Windows says about any unsigned program from the internet. Click **More info**, then **Run anyway**. It only asks once.

The exe is about 66 MB because it carries its own .NET runtime, so nothing else needs installing.

## Using it

Three tabs across the top: **Farming**, **Buyers** and **Resets**. The sun / moon button switches dark and light mode, the gear opens Settings.

### Farming

- **+ Add timer** opens a picker. Type to search, or choose a category and a thing. Farm animals let you pick how far to grow them (adolescent, adult, elder). The timer starts the moment you add it.
- Each row shows the name, a label box you can type your own note into (north patch, yak pen, whatever), the countdown, and when it will be ready.
- **Start** starts an idle or finished timer. It does nothing while a timer is running, so a stray click cannot restart one.
- **Reset** clears a running timer back to idle and asks you first. You can turn the confirmation off in Settings.
- **x** removes the row, and asks first. You can turn that off in Settings too.
- Ready timers turn green, jump to the top, play a sound and show a tray balloon. They stay until you Start or remove them.
- Minimise sends the app to the tray next to the clock. Double click the tray icon or right click, Open to bring it back. Close quits, but your timers are saved and keep counting while the app is shut.

### Buyers

The three Player-owned farm buyers (small, medium, large) reset on a fixed schedule rather than a countdown from when you last sold. The tab shows how long until each one next resets and the local clock time it happens. Nothing to start; it just runs.

### Resets

The weekly reset (Wednesday 00:00 UTC) and the monthly reset (the 1st at 00:00 UTC), each with a countdown and the local time. Under each is a checklist of the activities that reset then: penguins, Tears of Guthix, the circus, Troll Invasion, God Statues and so on. Tick them off as you do them. When the reset passes, the ticks clear on their own so you can see what is ready to do again.

The game clock is UTC on every world, so the countdowns are the same for everyone. The clock times are shown in your own time zone.

### Settings

Volume, mute, test sound, a custom alert sound (any WAV or MP3, only the first 10 seconds play, back to the built-in chime if the file goes missing), keep on top of the game, ask before reset, ask before remove, and where your files live.

## Where things are saved

- `rs3tracker-state.json` next to the exe holds your timers, checklist ticks and settings. If that folder is read only, it falls back to `%LocalAppData%\RS3Tracker`. Settings shows the path in use.
- End times are stored as UTC clock times, so reopening the app later shows the right remaining time.

## Changing or adding timers

The list of things you can track comes from `timers.json`, built into the exe. To change it without rebuilding, put a copy of [data/timers.json](data/timers.json) next to the exe (or in a `data` folder above it) and edit that. The app uses the external copy when it finds one.

Each category has a list of items. An item needs a `name` and either `minutes` (a plain countdown) or `stages` (cumulative targets, used for animals). `level` and `note` are optional and only shown in the picker.

```json
{ "name": "Potato", "minutes": 40, "level": 1, "note": "4 cycles of 10 min" }
```

Growth times come from the RuneScape Wiki. Farming patches grow on a fixed game clock, so the real ready time can be up to one cycle later than the countdown.

The Buyers and Resets rows come from the `clocks` list in the same file. A clock has a `group` (`buyers`, `weekly` or `monthly`, which tab and heading it sits under), a `kind` (`daily`, `weekly`, `monthly`, or `cycle` with an `anchor` date and `days`), a `note`, and optionally `items`, the checklist under it. Add a weekly activity by adding a line to the weekly clock's `items`:

```json
{ "name": "Penguin Hide and Seek", "note": "12+ points a week, each worth 10,000 gp or a lamp" }
```

## What it does not do yet

- Breeding timers.
- Other cooldowns such as Miscellania or divine locations. They would slot into the same list; suggestions welcome.
- Anything that needs the game itself. If Jagex ever ships an API, timers could be started from it.

## Building from source

Requires the .NET 8 SDK on Windows.

```
dotnet build -c Release        # check it compiles
publish.cmd                    # produces dist\RS3Tracker.exe, single file, self contained
```

C# and WPF, no third party packages. `MainWindow` is the timer list, `AddTimerWindow` the picker, `SettingsWindow` the settings, `FixedClock` the buyer and reset rows and their checklists, `Storage` finds the catalog and state files, `Sound` plays the alert, `Theme` and `DarkTitle` handle dark and light mode.

## Licence

[PolyForm Noncommercial 1.0.0](LICENSE). Free to use, copy, change and share. Selling it or using it commercially needs the copyright holder's permission.

Copyright (c) 2026 signalbrycea.

# Multitasking for Melvor Idle 2

Train every skill at the same time. A Melvor Idle 2 port of the idea behind the
[Melvor Idle 1 Multitasking mod](https://mod.io/g/melvoridle/m/multitasking).

> [!IMPORTANT]
> **Only tested with the Melvor Idle 2 alpha** (Steam playtest, v0.11.5-alpha.3, Windows). Melvor Idle 2 is in early
> development; a game update can break the mod at any time. Back up your saves
> (`%APPDATA%\Games By Malcs\Melvor Idle 2`) before playing with mods. This is an unofficial fan project, not
> affiliated with or supported by Games By Malcs.

> [!NOTE]
> **Made with [Claude Code](https://claude.com/claude-code).** The code and documentation in this repository were
> written with Anthropic's AI coding assistant, directed, reviewed and tested in-game by a human.

## Install

1. Install the **[Melvor Idle 2 Mod Loader](https://github.com/nachtmarv/melvor2-modloader#install)**: put
   `Melvor2Modded.exe` into the game folder and start it once. This creates the `mods` folder.
2. Download **[Multitasking.zip](https://github.com/nachtmarv/melvor2-multitasking/releases/latest/download/Multitasking.zip)**
   from the [latest release](https://github.com/nachtmarv/melvor2-multitasking/releases/latest).
3. Extract it into the `mods` folder in the game folder, so that you get `mods\Multitasking\mod.json`:

   ```
   Melvor Idle 2 Playtest/
     Melvor2Modded.exe
     mods/
       Multitasking/
         mod.json
         MultitaskingMod.dll
   ```
4. Start the game with **Melvor2Modded.exe**. The main menu lists *Multitasking* at the bottom left.

**Update:** delete `mods\Multitasking` and extract the new zip. **Uninstall:** delete `mods\Multitasking`.

## Features

- Start as many skills as you like; starting a skill only stops that skill's previous action.
- Every running skill shows its action progress ring on its sidebar button. Combat keeps the button at the top of the
  sidebar. The legacy sidebar (game setting) works too.
- The footer (current action, time to next level, gains/losses per hour) follows the skill page you are viewing.
- Offline progress runs all active skills; the "Welcome Back" summary scrolls horizontally to show them all.
- Food and HP work per skill: Combat, Thieving and Agility share your HP, but each one auto-eats its own selected food
  when it takes damage, and a death only stops the skill that died.

Saves made with several running skills also load without the mod; the skills keep running until you start something.

**After a game update:** if the update breaks part of the mod, that part switches itself off and the game behaves as
without it, while the rest of the mod keeps working. The game's log (`%APPDATA%\Games By Malcs\Melvor Idle 2\logs`)
names what was disabled (`[Multitasking] ... disabled`). Check for a new version of the mod then.

## Building from source

Requires the .NET 10 SDK and the game with the mod loader installed, by default the Steam install, found through the
registry (see `Directory.Build.props`; override with `-p:GameDir=...` / `-GameDir ...`). The mod compiles against the
game assembly and the loader in `<game>\modloader`. The mod loader's README explains how mods work.

- `scripts/install.ps1` builds the mod and deploys it to `<game>\mods\Multitasking`.
- `scripts/uninstall.ps1` removes it again.
- `scripts/package.ps1` builds the release zip `dist/Multitasking.zip`.
- `scripts/release.ps1` publishes a GitHub release (needs the [GitHub CLI](https://cli.github.com/)): it builds the
  zip, tags the commit `v<version>` and uploads the zip. The version is `<Version>` in
  `src/MultitaskingMod/MultitaskingMod.csproj`.

## License

[MIT](LICENSE). Melvor Idle 2 and its assets belong to Games By Malcs; this repository contains none of the game's
files.

# Anodyne Archipelago Client

[Archipelago](https://archipelago.gg/) is an open-source project that supports
randomizing a number of different games and combining them into one cooperative
experience. Items from each game are hidden in other games. For more information
about Archipelago, you can look at their website.

This is a project that modifies the game [Anodyne](https://www.anodynegame.com/)
so that it can be played as part of an Archipelago multiworld game.

## Installation

1. Download the Anodyne Archipelago Randomizer from
   [the releases page](https://code.fourisland.com/anodyne-archipelago/about/CHANGELOG.md).
2. Locate `AnodyneSharp.exe`. On Steam, you can find it by right clicking on
   "Anodyne", going to "Manage", and clicking "Browse local files". In the
   folder that opens, you should see another folder called "Remake".
   `AnodyneSharp.exe` should be in there.
3. Unzip the randomizer into the same folder as `AnodyneSharp.exe`. This should
   add two folders, one exe, and several dll files to this folder.

## Joining a Multiworld game

1. Run `AnodyneArchipelagoLauncher.exe`.
2. Enter your connection details on the main menu. Text must be entered via
   keyboard, even if you are playing on controller.
3. Select "Connect".
4. Enjoy!

To continue an earlier game, you can perform the exact same steps as above. The
randomizer will remember the details of your last nine unique connections.

## Running from source

If you are not testing local changes to the source code, skip this section and
read the Installation section instead.

### Steam Version

This is the `AnodyneArchipelago.BepInEx` sub-project.

The `AnodyneArchipelago` project depends on several other assemblies. These can
be found either in the BepInEx project or in the `AnodyneSharp.exe` folder. The
BepInEx assemblies used, as well as the mod launcher itself, must be compiled
for x64 (not Any CPU) and .NET Framework 4.6.2. You will need to clone the
BepInEx project and configure it so that they compile correctly.

The layout of the `AnodyneArchipelago` release is as follows:

- The `BepInEx.NET.Framework.Launcher` executable, renamed to
  `AnodyneArchipelagoLauncher.exe`.
- The BepInEx dependencies: `0Harmony.dll`, `BepInEx.Core.dll`,
  `BepInEx.NET.Common.dll`, `BepInEx.Preloader.Core.dll`, `Mono.Cecil.dll`,
  `Mono.Cecil.Mdb.dll`, `Mono.Cecil.Pdb.dll`, `Mono.Cecil.Rocks.dll`,
  `MonoMod.RuntimeDetour.dll`, `MonoMod.Utils.dll`, `SemanticVersioning.dll`.
- The `AnodyneArchipelago` dependencies: `Archipelago.MultiClient.Net.dll`,
  `Newtonsoft.Json.dll`.
- The `Resources` folder from the repository root.
- A folder called `BepInEx`, containing two subfolders: `config`, which contains
  `BepInEx.cfg` from the repository root, and `plugins`, which contains
  `AnodyneArchipelago.dll` itself.

### MonoGame Version

Modding support is being added to
[the MonoGame version of Anodyne](https://github.com/PixieCatSupreme/AnodyneSharp/).
There is experimental support for using this mod with it, using the
`AnodyneArchipelago.MonoGame` sub-project.

## Frequently Asked Questions

### Will this impact the base game?

The base game can still be played normally using `AnodyneSharp.exe` (i.e. by
launching the game through Steam). The randomizer will only be invoked when
running the game using `AnodyneArchipelagoLauncher.exe`. The randomizer also
uses separate save files from the main game, so your vanilla saves will not be
affected either.

### Is my progress saved locally?

The randomizer generates a savefile name based on your Multiworld seed and slot
number, so you should be able to seamlessly switch between multiworlds and even
slots within a multiworld.

The exception to this is different rooms created from the same multiworld seed.
The client is unable to tell rooms in a seed apart (this is a limitation of the
Archipelago API), so the client will use the same save file for the same slot in
different rooms on the same seed. You can work around this by manually moving or
removing the save file from the AnodyneFNA save file directory.

### How does Swap work?

The behavior of the Swap upgrade has been changed in the Archipelago mod. See
[extended_swap.md](https://github.com/hatkirby/AnodyneArchipelagoClient/blob/main/docs/extended_swap.md)
for more information.

### What about the wiggle glitch?

There is a technique in the base game where you can cross back and forth over a
screen transition while holding a directional key perpendicular to this
movement, and it will cause you to slowly move into solid geometry. This is
called the **wiggle glitch** (although it is not actually a glitch and is in
fact an intended mechanic).

The wiggle glitch can be used to bypass most progression barriers in the game,
including allowing you to reach the credits as soon as you have access to the
Fields area. Because of this, it is impossible to design randomizer logic that
includes the wiggle glitch, because allowing it would make almost everything in
logic immediately. Thus, the wiggle glitch is (almost) never is logic for
Archipelago.

There is one exception to this rule. In the secret top part of the Nexus, there
is a chest on an isolated platform. Swap is disabled in this area (both in the
base game and in the mod). The intended way of reaching this chest is by using
the wiggle glitch. Thus, you are expected to use the wiggle glitch in this room
to reach that chest. Any other use of the wiggle glitch is out-of-logic.

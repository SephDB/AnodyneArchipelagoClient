# Anodyne Archipelago Client

[Archipelago](https://archipelago.gg/) is an open-source project that supports
randomizing a number of different games and combining them into one cooperative
experience. Items from each game are hidden in other games. For more information
about Archipelago, you can look at their website.

This is a project that modifies the game [Anodyne](https://www.anodynegame.com/)
so that it can be played as part of an Archipelago multiworld game.

## Installation

The Anodyne Archipelago Client currently only supports
[the itch.io version](https://pixiecatsupreme.itch.io/anodyne-sharp) of the
game. The Steam version may be supported in the future.

1. Download the Anodyne Archipelago Randomizer from
   [the releases page](https://github.com/SephDB/AnodyneArchipelagoClient/releases).
2. Locate `AnodyneSharp.exe`.
3. Create a folder called `Mods` next to `AnodyneSharp.exe` if it does not
   already exist.
4. Unzip the randomizer into the `Mods` folder.

## Joining a Multiworld game

1. Open Anodyne.
2. Enter your connection details on the main menu. Text must be entered via
   keyboard, even if you are playing on controller.
3. Select "Connect".
4. Enjoy!

To continue an earlier game, you can perform the exact same steps as above. The
randomizer will remember the details of your last nine unique connections.

## Frequently Asked Questions

### Will this impact the base game?

The base game can still be played normally by not selecting "Archipelago" from
the main menu. You can also safely remove the randomizer from the `Mods` folder
and add it back later. The randomizer also uses separate save files from the
main game, so your vanilla saves will not be affected either.

### Is my progress saved locally?

The randomizer generates a savefile name based on your Multiworld seed and slot
number, so you should be able to seamlessly switch between multiworlds and even
slots within a multiworld.

The exception to this is different rooms created from the same multiworld seed.
The client is unable to tell rooms in a seed apart (this is a limitation of the
Archipelago API), so the client will use the same save file for the same slot in
different rooms on the same seed. You can work around this by manually moving or
removing the save file from the save file directory.

### How does Swap work?

The behavior of the Swap upgrade has been changed in the Archipelago mod. See
[extended_swap.md](https://github.com/SephDB/AnodyneArchipelagoClient/blob/main/docs/extended_swap.md)
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

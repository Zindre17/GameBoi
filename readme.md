# Gameboi
This project is an attempt at making a gameboy emulator(in C#), and is also my first attempt at making any emulator.

## Compatability
It should work on Windows, linux and macOS. It has been tested on Windows11 and Ubuntu 22.04.2 LTS.

## Issues
Some games does not function correctly.

## Controls
* __ESC -__ Load game ROM
* __W, A, S, D -__  Movement (Up, Left, Down, Right)
* __K -__ A-button
* __J -__ B-button
* __Enter -__ Start-button
* __Rigth Shift -__ Select-button

### Extras
* __Right Arrow -__ Speed up (x2 per step, with a max of x8)
* __Left Arrow -__ Slow down (/2 per step, with a min of x0.125)
* __Up Arrow -__ Volume up by 10%
* __Down Arrow -__ Volume down by 10%
* __M -__ Toggle mute sound
* __Space -__ Toggle pause emulation


## Future development plans
_These are not ordered by priority!_
Emulator 
- [x] Expand to GBC
- [x] Fix slow animations bug
- [x] Implement RTC registers (Done, but not fully. They just return the current time.)
- [x] Implement full state snapshots
- [x] Use cross-platform rendering of some sorts (OpenGL)
- [x] Cross platform sound
- [x] Improve Sound
- [ ] Improve performance
- [ ] Add Link Cable functionality (locally and over internet)
- [ ] Add Gameboy color's color mode for original Gameboy games (switchable preset palettes)
- [ ] Add configurable controls

Game specific issues
  - [x] Fix Pokemon Yellow rendering issues (black boxes)
  - [x] Fix Link's Awakening save-slots all maxed out on startup
  - [x] Fix Pokemon Trading Card Game not starting
  - [ ] Fix Pokemon Crystal rendering issues after intro + crash after new game initiation
  - [x] Fix Link's Awakening background displays wrong tiles after dialog boxes dissapear


## Information sources and other useful links
* [Pan Docs](http://bgb.bircd.org/pandocs.htm)
* [Game Boy CPU Manual](http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf)
* [Gameboy CPU (LR35902) instruction set](https://pastraiser.com/cpu/gameboy/gameboy_opcodes.html)
* [Game Boy Architecture](https://www.copetti.org/writings/consoles/game-boy/)
* [Game Boy Development Manual V1.1](https://archive.org/details/GameBoyProgManVer1.1)
* [Game Boy: Complete Technical Reference](https://gekkio.fi/files/gb-docs/gbctr.pdf)
* [Nitty Gritty Gameboy Cycle Timing](http://blog.kevtris.org/blogfiles/Nitty%20Gritty%20Gameboy%20VRAM%20Timing.txt)
* [A Look At The Game Boy Bootstrap: Let The Fun Begin!](https://realboyemulator.wordpress.com/2013/01/03/a-look-at-the-game-boy-bootstrap-let-the-fun-begin/)
* [BGB](http://bgb.bircd.org/)


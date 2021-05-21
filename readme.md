# Gameboi
This project is an attempt at making a gameboy emulator(in C#), and is also my first attempt at making any emulator.

## Compatability
It is only runnable on Windows as of now, due to using WinForms for creating UI. I Plan on updating to MAUI when that becomes available. Then it will also be runnable on Mac and Linux. 

## Issues
Some games does not function correctly. Of games tested, Pokemon Yellow have some sprite issues, and I suspect it has something to do with my MBC5-implementation.

## Controls
* __ESC -__ Load game ROM
* __W, A, S, D -__  Movement (Up, Left, Down, Right)
* __K -__ A-button
* __J -__ B-button
* __Enter -__ Start-button
* __Rigth Shift -__ Select-button

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


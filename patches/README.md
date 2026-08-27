# Pre-patched assemblies for How to Fish v1.0.9

These DLLs are taken from a verified working install. If you'd rather build the patch from source (so it stays compatible with future game updates), use `../tool/HTFHelper` and `../tool/Patcher` instead.

## Install

Copy the files from this folder into the game's `How to Fish_Data/Managed/` directory, overwriting the originals:

| From `patches/` | To `How to Fish_Data/Managed/` |
|---|---|
| `com.rlabrecque.steamworks.net.dll` | `com.rlabrecque.steamworks.net.dll` |
| `Assembly-CSharp.dll` | `Assembly-CSharp.dll` |
| `Heathen.Steamworks.dll` | `Heathen.Steamworks.dll` |
| `HTFHelper.dll` | `HTFHelper.dll` |

Then place `steam_appid.txt` and `playername.txt` from this folder next to `How to Fish.exe`.

## Build artifacts

| File | Source | Notes |
|---|---|---|
| `HTFHelper.dll` | `tool/HTFHelper/Helper.cs` (built with csc against game's `Managed/` refs) | Contains `HTF.HTFFake` (identity) and `HTF.HTFDirect` (LAN join) |
| `com.rlabrecque.steamworks.net.dll` | `tool/Patcher` against a clean game copy | 19 Steamworks methods faked |
| `Heathen.Steamworks.dll` | `tool/Patcher` against a clean game copy | All `Application.Quit` calls NOPed |
| `Assembly-CSharp.dll` | `tool/Patcher` against a clean game copy | Multiplayer / Join / Lobby-ID field rewrites |

The pre-patched files in this folder were verified working on Win 10 (LAN co-op, name change honoured, in-game chat).

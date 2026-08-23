# How to Fish — Multiplayer Patch (No Steam Required)

This is a patched build of **How to Fish** (v1.0.0) that removes the Steam dependency and adds **direct-IP multiplayer**, so you can play co-op with friends over LAN, a VPN, or the internet — no Steam account, no ownership check, no lobbies.

> Support the developers! If you enjoy the game, consider buying it on [Steam](https://store.steampowered.com/app/4001890/).

## Download

- **Patched full game:** [FullGame](https://vikingfile.com/f/ej4WPbu2NZ)

Extract the archive anywhere and run `How to Fish.exe`. The patch is already applied.

---

## How to Play

### 1. Set your name

Edit `playername.txt` in the game root folder :

```
PlayerABC
```

Rules: max **10 characters**, ASCII only (no spaces — use `_`, no diacritics). Your name is encoded into your in-game player ID, so **changing your name changes your save identity** — keep it stable.

### 2. Host a game (one player does this)

1. Main menu → **Create Game** → type a server name
2. Switch the mode toggle to **Multiplayer** (or Singleplayer — both work now)
3. Click **Create** → after the boat cutscene you are in the world, and the server is listening on **UDP 7777** on all network interfaces

### 3. Join (everyone else)

1. Main menu → **Join Game**
2. In the **Lobby ID** box, type the host's IP address:
   - Same LAN: `192.168.1.50:7777`
   - Radmin VPN / Hamachi: `26.x.x.x:7777`
   - Port can be omitted (defaults to `7777`): `192.168.1.50`
3. Click **Join**

### Playing over the internet

Pick one:

- **Radmin VPN (recommended, free)** — both players install it, join the same network, host shares their `26.x.x.x` IP
- **Port forwarding** — host forwards UDP 7777 on their router; friend connects using the host's public IP

In-game voice chat works automatically — it rides the same UDP connection.

---

## Command-line Options

| Argument | Effect |
|---|---|
| `--htfport 7788` | Change the server port (host) |
| `--htfjoin 1.2.3.4:7777` | Auto-join this address on startup |

You can also put the join address in a file named `ip.txt` next to the exe (lines starting with `#` are ignored).

---

## What the Patch Changes

The game originally requires Steamworks for auth, identity, chat names, and lobby hosting. The patch (applied via [Mono.Cecil](https://github.com/jbevain/cecil)) rewrites three assemblies and adds one helper library:

| Assembly | Changes |
|---|---|
| `com.rlabrecque.steamworks.net.dll` | 19 Steamworks API methods faked: player ID (encodes your name), persona names, app ID, achievements, overlays, language, `RestartAppIfNecessary` → `false`, etc. |
| `Heathen.Steamworks.dll` | `Application.Quit` calls removed (DRM relaunch + app-ID-mismatch checks) |
| `Assembly-CSharp.dll` | `CreateOfflineLobby` binds `0.0.0.0:7777` (was localhost-only); `JoinOfflineLobby` / `JoinByIDButton` accept `ip:port`; the Lobby ID input field is unlocked from digits-only to free text; the Multiplayer button now starts a direct-IP host instead of a Steam lobby |
| `HTFHelper.dll` *(new)* | All replacement logic: fake identity/name system, direct connect, config parsing |

Fun detail: since the game only syncs a `ulong` player ID between clients, the fake SteamID64 **encodes your display name** in its low 60 bits — so other players see your real name without any Steam friends list.

## Building the Patch Yourself

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and a **clean (unpatched) copy** of the game.

```bash
# 1. Build the helper library
cd patch-tool/HTFHelper
dotnet build -c Release
# -> bin/Release/HTFHelper.dll

# 2. Build & run the patcher
cd ../Patcher
dotnet build -c Release
dotnet run -c Release -- <folder-with-ORIGINAL-game-dlls> <folder-containing-HTFHelper.dll> <game-Managed-folder>
```

The patcher writes `steamworks.patched.dll`, `assembly-csharp.patched.dll`, and `heathen.patched.dll` into the output folder. To install:

1. Copy `steamworks.patched.dll` → `How to Fish_Data/Managed/com.rlabrecque.steamworks.net.dll`
2. Copy `assembly-csharp.patched.dll` → `How to Fish_Data/Managed/Assembly-CSharp.dll`
3. Copy `heathen.patched.dll` → `How to Fish_Data/Managed/Heathen.Steamworks.dll`
4. Copy `HTFHelper.dll` → `How to Fish_Data/Managed/`
5. Create `steam_appid.txt` containing `4001890` next to `How to Fish.exe`

`manual-dnspy/` contains the equivalent manual patches if you prefer editing with dnSpy.

## Troubleshooting

| Problem | Fix |
|---|---|
| Friend can't connect | Windows Firewall on the **host** is blocking UDP 7777 — allow the game or open the port |
| Joining drops back to menu | Wrong IP/port, or host hasn't entered the world yet |
| Name shows as "Fisher" | No `playername.txt` next to the exe |
| Progress reset | You changed your `playername.txt` — saves are keyed to the name-encoded ID |
| Game updated and patch stopped working | Re-run the patcher against the new DLLs (see *Building* above) |

## Credits

- Game by **Dazed Games**
- Patch, reverse engineering, and tooling: huynhhoang04
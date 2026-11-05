# Development Guide

How to set up, build, run, test and debug the digital version of The Subway Karma Game.

For the game rules the code implements, see [GAME_RULES.md](GAME_RULES.md).

---

## Requirements

| Requirement | Version | Needed for |
| --- | --- | --- |
| [Git](#1-git-and-git-lfs) | Any recent version | Everything |
| [Git LFS](#1-git-and-git-lfs) | 3.x | Everything. Board, card and spinner images are stored in LFS. |
| [Unity Hub](#2-unity-hub-and-a-unity-licence) | Latest | Installing and launching the Editor |
| [Unity Editor](#3-unity-editor-600068f1) | **6000.6.8f1** (Unity 6.6), as pinned in `SubwayKarmaGame/ProjectSettings/ProjectVersion.txt` | Everything |
| [Unity modules](#4-unity-modules) | Same version as the Editor | Android, Web and desktop builds |
| [Code editor](#5-code-editor) | VS Code + .NET SDK 8+, Visual Studio 2022, or Rider | Editing, IntelliSense, debugging |
| [Android device](#6-android-device-optional) | Android 8.0 (API 26)+, ARM64 | Optional: on-device testing |

Disk space: allow about **15 GB** for the Editor with Android and Web modules, plus about 2 GB for the project's `Library/` cache.

---

## Installing the requirements

Follow the steps in order. Each ends with a check so you know it worked.

### 1. Git and Git LFS

The repository stores images (`*.png`, `*.jpg`, audio and models) in [Git LFS](https://git-lfs.com). Without LFS, a clone contains small placeholder text files instead of images, and the game shows errors such as `Could not load 'spinner' from Resources` or a blank board.

1. Install Git:
   - **Windows:** [Git for Windows](https://git-scm.com/download/win), which includes Git LFS, or `winget install Git.Git`.
   - **macOS:** `brew install git git-lfs` (or Xcode Command Line Tools for Git, plus `brew install git-lfs`).
2. Turn on LFS for your user account (one time per machine):
   ```sh
   git lfs install
   ```
3. Clone the repository:
   ```sh
   git clone https://github.com/t3knoid/The-Subway-Karma-Board-Game.git
   ```
   If you cloned **before** installing LFS, run `git lfs pull` inside the repo to download the real files.

**Check:** `git lfs ls-files` lists files such as `SubwayKarmaGame/Assets/Resources/board.jpg`, and that file is hundreds of KB, not about 130 bytes.

> **Commit email:** if GitHub email privacy is on, pushes are rejected with `GH007: Your push would publish a private email address`. Set your noreply address (shown at https://github.com/settings/emails) before committing:
> `git config user.email "<id>+<username>@users.noreply.github.com"`

### 2. Unity Hub and a Unity licence

1. Download and install **Unity Hub** from https://unity.com/download.
2. Open Unity Hub and **sign in** with a Unity ID (create one for free if needed).
3. When prompted, or under **⚙ Preferences → Licenses → Add**, choose **Get a free personal licence**. Unity Personal is free for individuals and small teams under Unity's revenue threshold.

**Check:** Unity Hub's **Licenses** page shows an active licence.

### 3. Unity Editor 6000.6.8f1

The Editor version must match exactly. Opening the project in a different version triggers an upgrade that changes `ProjectSettings/` and `Packages/` for everyone. Don't accept an upgrade unless the team has agreed to move versions.

Pick one way to install it:

- **Easiest:** add the project first (see [Opening the project](#opening-the-project)). Unity Hub notices the missing version and offers **Install Version 6000.6.8f1**. Tick the modules from [step 4](#4-unity-modules) in the same dialog.
- **From Unity Hub:** **Installs → Install Editor**. If 6000.6.8f1 isn't under *Official releases*, open the [Unity download archive](https://unity.com/releases/editor/archive), find 6000.6.8f1 and click **Install** (it opens Unity Hub).
- **Direct link:** paste `unityhub://6000.6.8f1/237b6593792b` into your browser's address bar to open the install in Unity Hub.

The Editor is installed under `C:\Program Files\Unity\Hub\Editor\6000.6.8f1\` (Windows) or `/Applications/Unity/Hub/Editor/6000.6.8f1/` (macOS).

**Check:** **Installs** lists **6000.6.8f1**.

### 4. Unity modules

Modules are per-version add-ons that let the Editor build for other platforms. Add them in the install dialog, or later via **Installs → ⚙ next to 6000.6.8f1 → Add modules**.

| Module | Tick it if… | Notes |
| --- | --- | --- |
| **Android Build Support** | You build for Android (the primary target) | Also tick its sub-items **OpenJDK** and **Android SDK & NDK Tools**. Unity uses its own copies, so you don't need Android Studio. |
| **Web Build Support** | You build the browser version | Needed for the `WebGL/` (GitHub Pages) build |
| **Windows Build Support (IL2CPP)** / **Mac Build Support (IL2CPP)** | You want IL2CPP desktop builds | Optional. Desktop builds for your own OS work without it using Mono. On Windows, IL2CPP also needs Visual Studio's *Desktop development with C++* workload. |
| **Documentation** | You want the manual offline | Optional |

**Check:** in the Editor, **File → Build Profiles** shows Android (and Web) without a "No module loaded" message.

### 5. Code editor

The project already includes the Unity package that connects code editors (`com.unity.ide.visualstudio`), so you only need to install and configure the editor itself.

#### VS Code (recommended; the repo includes settings)

1. Install [VS Code](https://code.visualstudio.com).
2. Install the **.NET SDK 8 or later** from https://dotnet.microsoft.com/download (or `winget install Microsoft.DotNet.SDK.8`). The C# tooling needs the SDK, not just the runtime, to load the project.
3. Open the **`SubwayKarmaGame/`** folder in VS Code, not the repo root. That's where `.vscode/` lives. Accept the prompt to install the recommended **Unity** extension (`visualstudiotoolsforunity.vstuc`), which also installs **C#** and **C# Dev Kit**.
4. In Unity: **Edit → Preferences → External Tools → External Script Editor → Visual Studio Code**, then click **Regenerate project files**.

**Check:** `dotnet --list-sdks` shows an 8.x or later SDK. Hovering over `MonoBehaviour` in a script shows Unity documentation. **Run and Debug** lists **Attach to Unity** (from `.vscode/launch.json`).

#### Visual Studio 2022 (Windows)

1. In the Visual Studio Installer, select the **Game development with Unity** workload.
2. In Unity, set **External Script Editor** to *Visual Studio 2022* and **Regenerate project files**.

#### JetBrains Rider

1. Install Rider (Unity support is built in).
2. In Unity, install the **JetBrains Rider Editor** package (`com.unity.ide.rider`) from **Window → Package Manager**, then set **External Script Editor** to Rider.
3. That adds a line to `Packages/manifest.json`. Only commit it if the team agrees to keep both IDE packages.

### 6. Android device (optional)

1. On the phone, open **Settings → About phone** and tap **Build number** seven times to unlock **Developer options**.
2. In **Developer options**, turn on **USB debugging**.
3. Connect the phone by USB and accept the **Allow USB debugging?** prompt (tick *Always allow from this computer*).
4. **Windows only:** if the device isn't detected, install your phone maker's USB driver, or the [Google USB Driver](https://developer.android.com/studio/run/win-usb) for Pixel devices.

Unity's bundled `adb` is at:

- Windows: `C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`
- macOS: `/Applications/Unity/Hub/Editor/6000.6.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`

Add that folder to your `PATH` to use `adb` from a terminal.

**Check:** `adb devices` lists your device as `device` (not `unauthorized`). In Unity, **File → Build Profiles → Android → Run Device** shows the phone.

### Unity packages

You don't install these by hand. Unity downloads them from `SubwayKarmaGame/Packages/manifest.json` (exact versions pinned in `packages-lock.json`) the first time the project opens.

| Package | Used for |
| --- | --- |
| `com.unity.ugui` | Runtime UI: setup screen, score panel, results screen |
| `com.unity.inputsystem` | Installed. The game currently uses `OnMouseDown`, which works because *Active Input Handling* is set to **Both**. |
| `com.unity.test-framework` | Edit Mode unit tests |
| `com.unity.ide.visualstudio` | VS Code / Visual Studio integration and debugging |

To add or upgrade a package, use **Window → Package Manager** and commit both `manifest.json` and `packages-lock.json`.

---

## Repository layout

```
The-Subway-Karma-Game/
├── README.md                   Board-game overview (print-and-play)
├── docs/
│   ├── GAME_RULES.md           Authoritative rules spec for the digital version
│   ├── BUILDING.md             Per-platform build requirements and steps
│   └── DEVELOPMENT.md          This file
├── Subway Karma Game.pdf       Printable board + cut-out spinner (generated)
├── Karma Cards.pdf             Printable karma cards (generated)
├── assets/karma1-3.jpg         Card sheets as images (generated)
├── tools/                      Generators for board, cards, icons and printables (Python + Pillow)
└── SubwayKarmaGame/            ← Unity project (open this folder in Unity Hub)
    ├── Assets/
    │   ├── Resources/          Images loaded at runtime (board.jpg, spinner.png, spinner_arrow.png)
    │   ├── Scenes/MainGame.unity   The only scene (camera + lights; everything else is built in code)
    │   ├── Scripts/            Unity components (MonoBehaviours)
    │   │   └── Rules/          Pure C# rules engine (SubwayKarma.Rules assembly)
    │   └── Tests/EditMode/     NUnit tests for the rules engine
    ├── Packages/manifest.json
    └── ProjectSettings/
```

`Library/`, `Temp/`, `Logs/`, `UserSettings/` and build output are git-ignored. Unity regenerates them.

---

## Architecture

The code has two layers:

```
┌──────────────────────── Unity layer (Assembly-CSharp) ────────────────────────┐
│ GameBootstrap ──creates──► BoardManager, BoardDisplay, BoardSetup, Spinner,   │
│                            KarmaDeck, GameManager, TurnController,            │
│                            ScoreTracker, ResultsScreen, GameSetupScreen       │
│                                                                               │
│ GameSetupScreen ─► creates PlayerState(s) + PlayerToken(s) ─► GameManager     │
│ GameManager ─OnTurnChanged─► TurnController ─spin─► TurnResolver              │
│ TurnController ◄─TurnResult (events)─┘   animates tokens, updates PlayerState │
│ TurnController ─► GameManager.NextTurn() / TriggerEndGame() ─► ResultsScreen  │
└───────────────────────────────────────┬───────────────────────────────────────┘
                                        │ references
┌──────────────── Rules layer (SubwayKarma.Rules, no UnityEngine) ──────────────┐
│ StandardGame   board (22 squares) + deck (26 cards) data                      │
│ TurnResolver   ResolveSpin / ResolveBonusSpin → TurnResult (ordered events)   │
│ BoardSquare, KarmaCard, ICardSource                                           │
└───────────────────────────────────────────────────────────────────────────────┘
```

**Rule of thumb:** game rules go in `Scripts/Rules/` and get a test. Presentation code (animation, UI, input) goes in `Scripts/` and asks `TurnResolver` what happens. Never copy rules into a MonoBehaviour.

### Key scripts

| Script | Role |
| --- | --- |
| `GameBootstrap` | Runs automatically after the scene loads (`[RuntimeInitializeOnLoadMethod]`) and creates every manager, so the scene file stays nearly empty. |
| `GameSetupScreen` | Mode picker (Solo / vs Computer). Creates players and tokens, then calls `GameManager.StartGame()`. |
| `GameManager` | Owns the player list and turn index. Fires `OnTurnChanged`, `OnPlayerRegistered`, `OnGameOver`. |
| `TurnController` | Plays each turn: skip penalty → get spin (spinner tap or CPU roll) → resolve → animate → next turn. Shows the spinner only while a human must spin. |
| `Rules/TurnResolver` | All movement, card-draw and square-effect rules. Returns events (`Moved`, `CardDrawn`, `LoseNextTurn`, `BonusSpinRequired`, `DeckExhausted`). |
| `Rules/StandardGame` | Board layout and deck contents, the single source of truth for both. |
| `KarmaDeck` | Shuffled deck (Fisher-Yates); implements `ICardSource`. |
| `PlayerState` | Plain data per player: position, karma total, hand, `loseNextTurn`. |
| `PlayerToken` | 3D subway-car token built from primitives; `MoveTo` / `SnapTo` a square. |
| `SpinnerController` | Animated spinner; fires `OnSpinComplete(int)`. `Interactable` gates taps. |
| `BoardDisplay` / `BoardSetup` | Draws the board sprite and creates the `TapZone_00`–`TapZone_21` square colliders that tokens position against. |
| `ScoreTracker` / `ResultsScreen` | HUD score panel and end-of-game ranking overlay. |

---

## Opening the project

1. In **Unity Hub → Projects → Add → Add project from disk**, select the **`SubwayKarmaGame/`** folder inside the clone (not the repo root).
2. Open it. If the Editor version is missing, Unity Hub offers to install it (see [step 3](#3-unity-editor-600068f1)).
3. The first import builds `Library/` and downloads packages, which takes several minutes. Later opens are fast.
4. If Unity reports errors on first open, check the Console. Missing images usually mean Git LFS files weren't downloaded (`git lfs pull`).

**Check:** the Console has no red errors, and **Window → General → Test Runner → EditMode → Run All** passes.

---

## Running in the Editor

1. Open `Assets/Scenes/MainGame.unity`.
2. Press **Play**.
3. Choose **Solo Play** or **vs Computer**.
4. Click the spinner when it appears to take your turn. The computer's turns run automatically.

A game ends when all 26 karma cards are drawn. **Play Again** resets players and reshuffles the deck.

Tips:
- Set the *Game* view to a fixed resolution or aspect (e.g. 16:9 Landscape) to match target devices. The UI is designed for a 960×600 reference resolution.
- *Enter Play Mode Options* (Project Settings → Editor): because managers use static singletons and `DontDestroyOnLoad`, keep **Reload Domain** enabled. Disabling it leaves stale `Instance` references between plays.

---

## Testing

Unit tests cover the rules engine (`Assets/Tests/EditMode/TurnResolverTests.cs`): movement, station draws, every square effect, Express bonus moves, end-of-deck handling and a full simulated game.

**In the Editor:** *Window → General → Test Runner → EditMode → Run All*.

**From the command line** (CI or a terminal; the project must not be open in another Editor):

```sh
# Windows (Git Bash / PowerShell — adjust the Editor path)
"C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe" \
  -batchmode -nographics -projectPath SubwayKarmaGame \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode.xml -logFile -

# macOS
/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath SubwayKarmaGame \
  -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -
```

The exit code is non-zero if any test fails. Results are NUnit XML.

When you add or change a rule, add a test in `TurnResolverTests` and update `docs/GAME_RULES.md` in the same change.

---

## Building

See **[BUILDING.md](BUILDING.md)** for per-platform requirements and step-by-step builds: Android (APK and Play Store AAB), iOS, Web/GitHub Pages, Windows, macOS and Linux. It also covers the `Builds/<Platform>/` output layout, command-line builds and known issues to fix before a mobile release.

Quick local playtest: *File → Build Profiles → Windows → Build* into `SubwayKarmaGame/Builds/Windows/`.

---

## Debugging

### Logs

Every component logs with a bracketed prefix, so you can filter the *Console* window by system:

| Prefix | Shows |
| --- | --- |
| `[TurnController]` | Spins, penalties, Express bonus spins, turn hand-off |
| `[GameManager]` | Player registration, turn changes, final rankings |
| `[KarmaDeck]` | Every card drawn and cards remaining |
| `[PlayerState]` | Card points and running karma total |
| `[GameBootstrap]` | Which managers were created at startup |
| `[SpinnerController]`, `[SquareTap]` | Spinner results, square taps |

`TurnEvent.ToString()` prints readable events (e.g. `Moved 3 -> 5 (2)`, `CardDrawn @ 5 [Standard] Gets a Seat (+1)`) for quick logging of a `TurnResult`.

### Breakpoints in the Editor (VS Code)

1. Complete the [VS Code setup](#vs-code-recommended-the-repo-includes-settings) (Unity extension and .NET SDK).
2. In Unity, set VS Code as the external editor and **Regenerate project files**.
3. In VS Code, open the `SubwayKarmaGame/` folder, choose **Run and Debug → Attach to Unity**, and press F5.
4. Set breakpoints (e.g. in `TurnController.RunTurn` or `TurnResolver.ResolveSpin`) and press **Play** in Unity.

Visual Studio (*Attach to Unity*) and Rider (*Attach to Unity Editor*) work the same way. Unity may ask to switch to **Debug mode** (bug icon, bottom-right of the Editor). Accept it.

### Inspecting runtime state

All managers are created at runtime, so look under **DontDestroyOnLoad** in the *Hierarchy* during Play mode (`GameManager`, `TurnController`, `KarmaDeck`, `PlayerToken_Slot0`, …). `PlayerState` is plain C#, not a component, so inspect it with the debugger (`GameManager.Instance.Players`) or the logs.

### On an Android device

1. In *Build Profiles*, enable **Development Build**, **Script Debugging** and optionally **Wait For Managed Debugger**, then Build And Run.
2. Device logs: `adb logcat -s Unity` (from the Android SDK's `platform-tools`). The Editor's *Console* can also attach to a device from the *Editor* dropdown.
3. Attach the IDE debugger to the player instance listed in *Attach to Unity*. The device must be on the same network or connected over USB.
4. Use the **Profiler** (*Window → Analysis → Profiler*) with *Autoconnect Profiler* for performance issues.

### Debugging rules without Unity

The `SubwayKarma.Rules` assembly has no Unity dependency (`noEngineReferences: true`). A rule can be reproduced by writing a test in `TurnResolverTests` with a fixed position and spin, then stepping through it from the Test Runner with the debugger attached.

### Common problems

| Symptom | Likely cause / fix |
| --- | --- |
| Buttons don't respond | No `EventSystem`. `GameBootstrap.EnsureEventSystem()` should create one; check the Console for bootstrap errors. |
| Spinner never appears | It's only shown on a human player's turn. Check `[TurnController]` logs and that `GameManager.StartGame()` ran after mode selection. |
| Tokens sit at the world origin | `PlayerToken` positions on `TapZone_XX` objects; `BoardSetup` must have created them. |
| `Could not load 'spinner' from Resources`, or a blank board | Usually Git LFS placeholders instead of images: run `git lfs install` then `git lfs pull`. Otherwise, images must stay in `Assets/Resources/` with their exact names. |
| Unity Hub says the project uses a different Editor version | Install **6000.6.8f1** (see [step 3](#3-unity-editor-600068f1)) rather than upgrading the project. |
| `git push` rejected with `GH007` | Commits carry a private email. Set your GitHub noreply address (see [step 1](#1-git-and-git-lfs)). |
| Script errors after pulling | A `.meta` file is missing or was regenerated. Commit `.meta` files with every new asset or script. |
| Test Runner shows no tests | `com.unity.test-framework` didn't resolve, or the test assembly failed to compile. Check the Console. |

---

## Contributing conventions

- Work on a feature branch and merge to `main` through a pull request.
- Commit messages follow Conventional Commits: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`.
- Always commit the `.meta` file alongside any new or moved asset, script or folder. Move files inside Unity (or move the `.meta` with them) so GUIDs are preserved.
- Rules changes need a test and an update to `docs/GAME_RULES.md`.

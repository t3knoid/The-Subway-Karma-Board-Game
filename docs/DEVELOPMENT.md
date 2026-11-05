# Development Guide

How to set up, build, run, test and debug the digital version of The Subway Karma Game.

For the game rules the code implements, see [GAME_RULES.md](GAME_RULES.md).

---

## Requirements

| Requirement | Version / notes |
| --- | --- |
| Unity Editor | **6000.6.8f1** (Unity 6.4), the version in `SubwayKarmaGame/ProjectSettings/ProjectVersion.txt`. Install through Unity Hub. |
| Unity Hub modules | **Android Build Support** (includes OpenJDK and Android SDK/NDK) for Android builds. **WebGL Build Support** for web builds. Windows/Mac Build Support for desktop builds. |
| Code editor | VS Code with the **Unity** extension (`visualstudiotoolsforunity.vstuc`), or Visual Studio 2022 with the *Game development with Unity* workload, or JetBrains Rider. |
| c | `com.unity.ide.visualstudio` (for VS Code / Visual Studio). It is **not** in the manifest yet; add it from *Window → Package Manager*, or install `com.unity.ide.rider` for Rider. |
| Git | Any recent version. `.gitattributes` handles line endings. |
| Android device (optional) | Android 8.0 (API 26) or newer, ARM64, with USB debugging enabled. |

Unity packages the project depends on (see `SubwayKarmaGame/Packages/manifest.json`):

- `com.unity.ugui`: runtime UI (setup screen, score panel, results screen)
- `com.unity.inputsystem`: installed; the game currently uses `OnMouseDown`, which works because *Active Input Handling* is set to **Both**
- `com.unity.test-framework`: Edit Mode unit tests

---

## Repository layout

```
The-Subway-Karma-Game/
├── README.md                   Board-game overview (print-and-play)
├── docs/
│   ├── GAME_RULES.md           Authoritative rules spec for the digital version
│   └── DEVELOPMENT.md          This file
├── Subway Karma Game.pdf/.pub  Original printable board
├── Karma Cards.pdf/.pub        Original printable cards
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

## Getting started

1. Clone the repository:
   ```sh
   git clone https://github.com/t3knoid/The-Subway-Karma-Game.git
   ```
2. In **Unity Hub → Add → Add project from disk**, select the `SubwayKarmaGame/` folder (not the repo root).
3. Open it with Unity **6000.4.0f1**. The first import builds `Library/` and resolves packages, which takes a few minutes.
4. Set your code editor: *Edit → Preferences → External Tools → External Script Editor* (VS Code / Visual Studio / Rider). Click **Regenerate project files** if IntelliSense is missing.

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
"C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe" \
  -batchmode -nographics -projectPath SubwayKarmaGame \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode.xml -logFile -

# macOS
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath SubwayKarmaGame \
  -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -
```

The exit code is non-zero if any test fails. Results are NUnit XML.

When you add or change a rule, add a test in `TurnResolverTests` and update `docs/GAME_RULES.md` in the same change.

---

## Building

All builds use the single scene `Assets/Scenes/MainGame.unity`, which is already in *File → Build Profiles*.

### Android (primary target)

Current player settings: product name **Subway Karma**, company **t3knoid**, min API **26**, architecture **ARM64**, auto-rotation.

1. *File → Build Profiles → Android → Switch Platform*.
2. *Player Settings → Other Settings*: set a **Package Name** (e.g. `com.t3knoid.subwaykarma`). It's unset, so Unity generates a default. Keep **Scripting Backend = IL2CPP** (required for ARM64).
3. Connect a device with USB debugging on, then **Build And Run**, or **Build** to produce an `.apk`.
4. For a Play Store upload, enable *Build App Bundle (Google Play)* and configure a keystore under *Publishing Settings*. `.apk`, `.aab` and keystores must never be committed.

### WebGL

1. *File → Build Profiles → Web → Switch Platform*.
2. **Build** into `WebGL/` (git-ignored; reserved for GitHub Pages deployment). Use **Build And Run** to have Unity serve it on a local web server. Opening `index.html` directly from disk does not work.

### Desktop (Windows / macOS)

*File → Build Profiles → Windows/Mac → Build*, output to `Builds/` (git-ignored). Useful for quick playtests without a device.

### Command-line builds

Unity has no built-in "build this profile" CLI switch, so a scripted build needs a small editor script (e.g. `Assets/Editor/BuildScript.cs` with a static method calling `BuildPipeline.BuildPlayer`) invoked with:

```sh
Unity -batchmode -quit -projectPath SubwayKarmaGame \
  -buildTarget Android -executeMethod BuildScript.BuildAndroid -logFile -
```

The repository doesn't have one yet. Add it when CI is set up.

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

1. Install the `com.unity.ide.visualstudio` package and the VS Code **Unity** extension.
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
| `Could not load 'spinner' from Resources` | Images must stay in `Assets/Resources/` with their exact names. |
| Script errors after pulling | A `.meta` file is missing or was regenerated. Commit `.meta` files with every new asset or script. |
| Test Runner shows no tests | `com.unity.test-framework` didn't resolve, or the test assembly failed to compile. Check the Console. |

---

## Contributing conventions

- Work on a feature branch and merge to `main` through a pull request.
- Commit messages follow Conventional Commits: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`.
- Always commit the `.meta` file alongside any new or moved asset, script or folder. Move files inside Unity (or move the `.meta` with them) so GUIDs are preserved.
- Rules changes need a test and an update to `docs/GAME_RULES.md`.

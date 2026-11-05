# Building Subway Karma

How to produce playable builds of the Unity project for each platform, and what each one needs.

For first-time setup (Git LFS, Unity Hub, the Editor and code editors), start with [DEVELOPMENT.md](DEVELOPMENT.md#installing-the-requirements). This guide assumes the project already opens in Unity **6000.6.8f1** without errors.

---

## At a glance

| Target | Build from Windows? | Unity module (for 6000.6.8f1) | Extra tools / accounts | Cost to publish |
| --- | --- | --- | --- | --- |
| [Android](#android) | ✅ | Android Build Support + **OpenJDK** + **Android SDK & NDK Tools** | Android phone with USB debugging (for testing) | Google Play: $25 one-time |
| [iOS / iPadOS](#ios-and-ipados) | ⚠️ Xcode project only | iOS Build Support | **Mac with Xcode**, Apple ID | Apple Developer Program: $99/year |
| [Web (WebGL)](#web-webgl) | ✅ | Web Build Support | A static web host (e.g. GitHub Pages) | Free |
| [Windows](#windows) | ✅ | Included (Mono). *Windows Build Support (IL2CPP)* is optional. | None (IL2CPP also needs Visual Studio's C++ workload) | Free; stores optional |
| [macOS](#macos) | ⚠️ Unsigned only | Mac Build Support (Mono) | Mac + Apple Developer account to sign and notarize | $99/year to notarize |
| [Linux](#linux) | ✅ | Linux Build Support (Mono) | None | Free |

**Adding a module:** Unity Hub → **Installs** → ⚙ next to **6000.6.8f1** → **Add modules**. A platform with no module shows "No module loaded" in *File → Build Profiles*.

**Mono vs IL2CPP:** these are the two scripting backends. **Mono** is fast to build and the default on desktop. **IL2CPP** compiles C# to native C++. It's slower to build but faster at runtime, and it's required for Android ARM64 and iOS.

---

## Before any build

1. **Be on an up-to-date `main`** (or your feature branch) with a clean Console: no red errors.
2. **Run the tests:** *Window → General → Test Runner → EditMode → Run All*.
3. **Check the scene list:** *File → Build Profiles → Scene List* must contain `Assets/Scenes/MainGame.unity` (already set). It's the only scene; everything else is created at runtime by `GameBootstrap`.
4. **Bump the version** for anything you'll share: *Player Settings → Version* (currently `1.0`). On Android, also increase *Bundle Version Code* for every Play upload.

### Current player settings

| Setting | Value | Where |
| --- | --- | --- |
| Product name / company | Subway Karma / t3knoid | Player Settings |
| Version | 1.0 | Player Settings → Other Settings |
| Orientation | Auto Rotation, **landscape only** (Landscape Left and Right; portrait disabled) | Player Settings → Resolution and Presentation |
| Active input handling | Both (legacy Input Manager + Input System) | Player Settings → Other Settings |
| Render pipeline | Built-in | Project Settings → Graphics |
| Android min API / architecture | 26 (Android 8.0) / ARM64 | Player Settings → Other Settings |
| Android package name | **Not set** (Unity generates a default) | Player Settings → Other Settings |
| Web compression | Gzip, no decompression fallback | Player Settings → Publishing Settings |

---

## Output folders

Build into `SubwayKarmaGame/Builds/<Platform>/`, never inside `Assets/`. Everything under `Builds/` is git-ignored, as are `*.apk`, `*.aab` and `WebGL/`.

```
SubwayKarmaGame/
└── Builds/
    ├── Android/   SubwayKarma.apk  ·  SubwayKarma.aab
    ├── WebGL/     index.html, Build/, TemplateData/
    ├── Windows/   Subway Karma.exe, Subway Karma_Data/, UnityPlayer.dll, …
    ├── macOS/     Subway Karma.app
    └── Linux/     SubwayKarma.x86_64, SubwayKarma_Data/
```

Unity remembers the last folder per build profile on each machine, but that memory isn't committed. Choose these folders the first time you build each platform.

> Files and folders whose names end in `_BurstDebugInformation_DoNotShip` or `_BackUpThisFolder_ButDontShipItWithYourGame` are debugging aids. Don't include them when you distribute a build.

---

## Android

**Needs:** the Android Build Support module (with the OpenJDK and SDK & NDK sub-modules) and, for testing, a phone set up as in [DEVELOPMENT.md → Android device](DEVELOPMENT.md#6-android-device-optional).

### One-time setup

In *File → Build Profiles → Android → Player Settings → Other Settings*:

| Setting | Set to | Why |
| --- | --- | --- |
| **Package Name** | e.g. `com.t3knoid.subwaykarma` | Unique app ID. It can't change after the app is published. |
| **Scripting Backend** | IL2CPP | Required for ARM64 |
| **Target Architectures** | ARM64 (already set) | Google Play requires 64-bit |
| **Minimum API Level** | 26 (already set) | Android 8.0+ |
| **Target API Level** | Automatic (highest installed) | Keeps up with Google Play's required target level |

### Test build (APK)

1. *File → Build Profiles → Android → **Switch Platform***. The first switch re-imports assets and takes a few minutes.
2. Connect the phone. It should appear under **Run Device**.
3. Click **Build And Run** and save as `Builds/Android/SubwayKarma.apk`. Unity installs and launches it on the phone.

To install an existing APK by hand: `adb install -r Builds/Android/SubwayKarma.apk`.

### Google Play build (AAB)

1. Tick **Build App Bundle (Google Play)** in the Android build profile.
2. *Player Settings → Publishing Settings → **Keystore Manager***: create a keystore and key. **Back up the keystore and passwords somewhere safe.** If you lose them, you can't ship updates to the same app. Never commit them to the repository.
3. Increase **Bundle Version Code** for every upload.
4. **Build** as `Builds/Android/SubwayKarma.aab` and upload it in the Google Play Console. Google Play App Signing is recommended.

---

## iOS and iPadOS

**Needs:** the iOS Build Support module, **a Mac with Xcode** (a version that supports Unity 6.6), and an Apple ID. TestFlight and the App Store require the paid Apple Developer Program.

Unity on Windows can only generate an **Xcode project**. The app itself is compiled, signed and installed from Xcode on a Mac.

1. *File → Build Profiles → iOS → Switch Platform*.
2. *Player Settings → Other Settings*: set a **Bundle Identifier** (e.g. `com.t3knoid.subwaykarma`) and **Target minimum iOS Version**.
3. **Build** into `Builds/iOS/`. This produces an Xcode project.
4. On a Mac, open `Builds/iOS/Unity-iPhone.xcodeproj`. Under *Signing & Capabilities* choose your **Team**, connect an iPhone, and press **Run**.

Notes:
- With a **free** Apple ID, apps installed on your own device stop working after 7 days.
- No Mac? Cloud build services (e.g. Unity Build Automation, Codemagic) can run the Xcode step.

---

## Web (WebGL)

**Needs:** the Web Build Support module. Works from Windows.

1. *File → Build Profiles → Web → Switch Platform*.
2. **Build And Run** to test. Unity serves the build on a local web server and opens your browser. Opening `index.html` straight from disk does **not** work.
3. For publishing, **Build** into `Builds/WebGL/`.

### Hosting on GitHub Pages

GitHub Pages can't send the `Content-Encoding` headers Unity's compressed builds expect. Use one of these:
- *Player Settings → Publishing Settings*: keep **Compression Format = Gzip** (current) and turn on **Decompression Fallback**.
- Or set **Compression Format = Disabled**. This gives larger downloads but no server requirements.

Then publish the *contents* of `Builds/WebGL/` to GitHub Pages:
- **Manually:** push them to a `gh-pages` branch and enable Pages for that branch in the repository settings.
- **Automatically:** a GitHub Actions workflow (e.g. using GameCI) that builds WebGL and deploys it. This needs a Unity licence stored as repository secrets.

Keep web builds out of `main`.

---

## Windows

**Needs:** nothing extra for Mono builds.

1. *File → Build Profiles → Windows → Switch Platform* if it isn't already active.
2. **Build** into `Builds/Windows/`. The output is `Subway Karma.exe` plus `Subway Karma_Data/`, `UnityPlayer.dll`, `MonoBleedingEdge/` and other support files.
3. Share it by zipping the **whole folder**. The `.exe` won't run on its own.

Optional:
- **IL2CPP** (*Player Settings → Scripting Backend*): requires the *Windows Build Support (IL2CPP)* module and Visual Studio's **Desktop development with C++** workload.
- **Development Build** (in the build profile) adds a debug console and allows the debugger and Profiler to attach.
- Logs from a running build are written to `%USERPROFILE%\AppData\LocalLow\t3knoid\Subway Karma\Player.log`.

---

## macOS

**Needs:** the Mac Build Support (Mono) module. Building from Windows works, but the result is **unsigned**.

1. *File → Build Profiles → macOS → Switch Platform*.
2. **Build** into `Builds/macOS/`. The output is `Subway Karma.app`.
3. Test it on a Mac. Unsigned apps show *"cannot be opened because the developer cannot be verified"*. For your own testing: right-click the app → **Open**, or run `xattr -cr "Subway Karma.app"`.

To distribute without warnings, the app must be **code-signed** with a *Developer ID Application* certificate and **notarized** by Apple. Both need a Mac and the paid Apple Developer Program. Building on a Mac also allows IL2CPP and a universal (Intel + Apple silicon) build.

---

## Linux

**Needs:** the Linux Build Support (Mono) module.

1. *File → Build Profiles → Linux → Switch Platform*.
2. **Build** into `Builds/Linux/`. The output is an executable (`SubwayKarma.x86_64`) plus `SubwayKarma_Data/`.
3. On Linux, make it executable if needed (`chmod +x SubwayKarma.x86_64`) and run it. Zip the whole folder to share.

---

## Command-line builds

Command-line builds are useful for CI and repeatable releases. Unity has no switch for "build the active profile", so this needs a small editor script that calls `BuildPipeline.BuildPlayer`. **The repository doesn't have one yet.** A typical version looks like this:

```csharp
// Assets/Editor/BuildScript.cs (editor-only; not included in builds)
using UnityEditor;

public static class BuildScript
{
    static readonly string[] Scenes = { "Assets/Scenes/MainGame.unity" };

    [MenuItem("Build/Windows")]
    public static void BuildWindows() =>
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Subway Karma.exe");

    [MenuItem("Build/Android APK")]
    public static void BuildAndroid() =>
        Build(BuildTarget.Android, "Builds/Android/SubwayKarma.apk");

    [MenuItem("Build/WebGL")]
    public static void BuildWebGL() =>
        Build(BuildTarget.WebGL, "Builds/WebGL");

    [MenuItem("Build/macOS")]
    public static void BuildMac() =>
        Build(BuildTarget.StandaloneOSX, "Builds/macOS/Subway Karma.app");

    static void Build(BuildTarget target, string path)
    {
        var report = BuildPipeline.BuildPlayer(Scenes, path, target, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception($"Build failed: {report.summary.result}");
    }
}
```

Run it without opening the Editor UI. Close any Editor that already has the project open first.

```sh
# Windows
"C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe" \
  -batchmode -nographics -quit -projectPath SubwayKarmaGame \
  -buildTarget Android -executeMethod BuildScript.BuildAndroid \
  -logFile Builds/android-build.log

# macOS
/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath SubwayKarmaGame \
  -buildTarget StandaloneOSX -executeMethod BuildScript.BuildMac \
  -logFile Builds/mac-build.log
```

A non-zero exit code means the build failed. Check the log file. Signing secrets (Android keystore passwords, Apple certificates) belong in CI secrets, never in the script.

---

## Known issues to fix before a mobile release

Already handled:

- **Screen rotation:** the game is landscape-only, and the board, tokens and spinner re-fit when a phone flips between the two landscape directions or a desktop window is resized.
- **Notches and rounded corners:** the score panel and the spinner (its automatic spot and its drag limits) stay inside `Screen.safeArea`, via `SafeAreaFitter`. Put any new HUD element under a `SafeAreaFitter.Create(canvas)` area rather than directly on the canvas.
- **Original board artwork:** `Assets/Resources/board.jpg` is drawn by `tools/make_board.py`. It has the same layout as the original board, with invented subway-style lines (diamond markers, own names and letters), an original stylised map, and real New York neighbourhood names that aren't real station names. Re-run `python -I tools/make_board.py SubwayKarmaGame/Assets/Resources/board.jpg` after editing it. Keep the square geometry, which `BoardSetup.cs` depends on.
- **README image and printable board:** `tools/make_printables.py` regenerates `subway_karma.png` and `Subway Karma Game.pdf` (page 1 board, page 2 cut-out spinner) from the same board artwork.
- **Printable karma cards:** `tools/make_cards.py` reads the deck from `StandardGame.cs` and regenerates `assets/karma1-3.jpg`, `Karma Cards.pdf` (Avery 5371 layout, print at 100% scale) the in-game card back `Assets/Resources/card_back.png` and the in-game card faces `Assets/Resources/CardFaces/` (one per card, named from the card title). Re-run it whenever a card changes in code.
- **Player tokens:** `tools/make_tokens.py` draws the four subway-car tokens and their shadow into `Assets/Resources/Tokens/`.
- **App icon and splash screen:** artwork lives in `Assets/Art/Branding/` (default icon, Android adaptive foreground/background layers, splash logo), generated from the board's title card by `tools/make_branding.py`. Apply it with **Tools → Subway Karma → Apply App Icons and Splash Screen**. That sets every platform's icon and a splash screen showing the title card on charcoal, without the Unity logo. Re-run it after changing the artwork, then commit `ProjectSettings.asset`.

Still to do:

| Issue | Why it matters | Fix |
| --- | --- | --- |
| **Touch input** | Spinning and dragging use mouse events, which Unity also generates from single touches. Expected to work, but untested on a device. | Test tap, drag and press-and-hold-to-reset on a real phone. |

---

## Troubleshooting builds

| Symptom | Likely cause / fix |
| --- | --- |
| Platform shows **"No module loaded"** | Add the module for **6000.6.8f1** in Unity Hub (see [At a glance](#at-a-glance)). |
| Build works in the Editor but crashes or misbehaves as a player | Check `Player.log` (path above for Windows; `adb logcat -s Unity` for Android). Editor-only behaviour such as `Shader.Find` on shaders no asset references fails in builds. |
| The build doesn't include your latest changes | The build didn't finish or went to another folder. Check the Console for **Build completed** / errors, and check the files' modified times in `Builds/<Platform>/`. |
| Android: **device not listed** | USB debugging off, prompt not accepted, or a missing USB driver. Check with `adb devices`. |
| Android: **"Gradle build failed"** | Usually the SDK/NDK/JDK. Re-add the OpenJDK and Android SDK & NDK sub-modules, and make sure *Preferences → External Tools* uses the Unity-installed copies. |
| Web build shows a blank page or a decompression error | Opened from disk, or the host doesn't send compression headers. Use Build And Run locally; for hosting, enable Decompression Fallback or disable compression. |
| macOS: **"app is damaged / can't be opened"** | Unsigned build downloaded from the internet. Run `xattr -cr "Subway Karma.app"` for testing, or sign and notarize it. |

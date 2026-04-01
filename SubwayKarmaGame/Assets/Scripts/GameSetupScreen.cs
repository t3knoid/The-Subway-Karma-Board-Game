using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Entry-point screen shown at game start that lets the player choose a mode:
///   • Solo Play   — single human player (solitaire, existing behaviour)
///   • vs Computer — human + RNG-based CPU opponent
///
/// Disables the Spinner until a mode is selected (so the spinner cannot be
/// tapped before game mode is set). Built entirely at runtime; no scene YAML.
///
/// sortingOrder = 300 — renders above ScoreTracker (100) and ResultsScreen (200).
/// </summary>
public class GameSetupScreen : MonoBehaviour
{
    private GameObject _panel;

    // -- Lifecycle ----------------------------------------------------------
    void Start()
    {
        BuildUI();

        // Prevent the spinner from being interacted with before setup is done.
        if (SpinnerController.Instance != null)
            SpinnerController.Instance.gameObject.SetActive(false);
    }

    // -- UI construction ----------------------------------------------------
    private void BuildUI()
    {
        // Root canvas
        var canvas          = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 600f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 1f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Semi-opaque backdrop panel
        _panel = new GameObject("SetupPanel");
        _panel.transform.SetParent(transform, false);

        var panelRT           = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin     = Vector2.zero;
        panelRT.anchorMax     = Vector2.one;
        panelRT.offsetMin     = Vector2.zero;
        panelRT.offsetMax     = Vector2.zero;

        var panelImg  = _panel.AddComponent<Image>();
        panelImg.color = new Color(0.04f, 0.04f, 0.18f, 0.93f);

        // Title
        MakeLabel("Subway Karma Game", 40, new Vector2(0f, 120f), Color.white);

        // Mode buttons
        MakeButton("Solo Play",
            "One player — solitaire karma journey.",
            new Vector2(0f, 20f),
            new Color(0.18f, 0.45f, 0.82f),
            OnSoloPlay);

        MakeButton("vs Computer",
            "You vs. the computer.",
            new Vector2(0f, -70f),
            new Color(0.55f, 0.20f, 0.75f),
            OnVsComputer);
    }

    // -- Widget helpers -----------------------------------------------------

    private void MakeLabel(string text, int fontSize, Vector2 pos, Color color)
    {
        var go = new GameObject("TitleLabel");
        go.transform.SetParent(_panel.transform, false);

        var rt           = go.AddComponent<RectTransform>();
        rt.sizeDelta     = new Vector2(600f, 60f);
        rt.anchoredPosition = pos;

        var lbl           = go.AddComponent<Text>();
        lbl.text          = text;
        lbl.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize      = fontSize;
        lbl.alignment     = TextAnchor.MiddleCenter;
        lbl.color         = color;
        lbl.fontStyle     = FontStyle.Bold;
    }

    private void MakeButton(string label, string subtitle, Vector2 pos,
                             Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"{label}_Btn");
        go.transform.SetParent(_panel.transform, false);

        var rt           = go.AddComponent<RectTransform>();
        rt.sizeDelta     = new Vector2(320f, 60f);
        rt.anchoredPosition = pos;

        var img   = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var cb = new ColorBlock
        {
            normalColor      = bgColor,
            highlightedColor = bgColor * 1.2f,
            pressedColor     = bgColor * 0.8f,
            selectedColor    = bgColor,
            disabledColor    = bgColor * 0.5f,
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };
        btn.colors = cb;

        btn.onClick.AddListener(onClick);

        // Primary label
        var txtGO = new GameObject("BtnText");
        txtGO.transform.SetParent(go.transform, false);

        var txtRT         = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin   = new Vector2(0f, 0.4f);
        txtRT.anchorMax   = Vector2.one;
        txtRT.offsetMin   = txtRT.offsetMax = Vector2.zero;

        var txt       = txtGO.AddComponent<Text>();
        txt.text      = label;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;
        txt.fontStyle = FontStyle.Bold;

        // Subtitle
        var subGO = new GameObject("BtnSub");
        subGO.transform.SetParent(go.transform, false);

        var subRT       = subGO.AddComponent<RectTransform>();
        subRT.anchorMin = Vector2.zero;
        subRT.anchorMax = new Vector2(1f, 0.45f);
        subRT.offsetMin = subRT.offsetMax = Vector2.zero;

        var sub       = subGO.AddComponent<Text>();
        sub.text      = subtitle;
        sub.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sub.fontSize  = 13;
        sub.alignment = TextAnchor.MiddleCenter;
        sub.color     = new Color(1f, 1f, 1f, 0.7f);
    }

    // -- Button handlers ----------------------------------------------------

    private void OnSoloPlay()
    {
        Debug.Log("[GameSetupScreen] Solo Play selected.");
        StartGame(vsComputer: false);
    }

    private void OnVsComputer()
    {
        Debug.Log("[GameSetupScreen] vs Computer selected.");
        StartGame(vsComputer: true);
    }

    private void StartGame(bool vsComputer)
    {
        if (vsComputer)
            CreateComputerOpponent();

        // Re-enable spinner — human always goes first (index 0).
        if (SpinnerController.Instance != null)
            SpinnerController.Instance.gameObject.SetActive(true);

        // Hide setup overlay.
        _panel.SetActive(false);
        gameObject.SetActive(false);

        Debug.Log($"[GameSetupScreen] Game started. vsComputer={vsComputer}");
    }

    // -- Computer opponent setup -------------------------------------------

    private void CreateComputerOpponent()
    {
        // PlayerState for CPU
        var cpStateGO = new GameObject("ComputerState");
        Object.DontDestroyOnLoad(cpStateGO);
        var cpState           = cpStateGO.AddComponent<PlayerState>();
        cpState.playerId      = 1;
        cpState.playerName    = "CPU";
        cpState.playerType    = PlayerType.Computer;

        // ComputerPlayer controller
        var cpCtrlGO = new GameObject("ComputerPlayer");
        Object.DontDestroyOnLoad(cpCtrlGO);
        var cpCtrl         = cpCtrlGO.AddComponent<ComputerPlayer>();
        cpCtrl.playerState = cpState;   // set before Start() runs

        // Register with GameManager (ScoreTracker will pick up the new row too).
        GameManager.Instance?.RegisterPlayer(cpState);
        GameManager.Instance?.RegisterComputerPlayer(cpCtrl);

        Debug.Log("[GameSetupScreen] CPU opponent created and registered.");
    }
}

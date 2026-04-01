using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-game results overlay. Hidden until GameManager.OnGameOver fires.
///
/// Shows:
///   - Title "Game Over"
///   - Each player ranked by karma (winner highlighted in gold)
///   - Solitaire karma rating based on final score thresholds
///   - "Play Again" button — resets game state and hides this screen
///   - "Main Menu" button — placeholder (logs; extend when main menu scene exists)
///
/// Built entirely at runtime; no scene YAML required.
/// </summary>
public class ResultsScreen : MonoBehaviour
{
    // -- Karma rating thresholds (solitaire) --------------------------------
    private const int RatingEnlightened = 20;   // >= 20  → Enlightened
    private const int RatingNeutral     =  0;   // >= 0   → Neutral
    // anything below 0                          → Troubled

    // -- Layout constants ---------------------------------------------------
    private const float PanelW      = 500f;
    private const float PanelH      = 420f;
    private const float TitleH      =  60f;
    private const float RowH        =  36f;
    private const float ButtonW     = 180f;
    private const float ButtonH     =  48f;
    private const float Padding     =  16f;

    private static readonly Color BgColor      = new Color(0.05f, 0.05f, 0.1f, 0.93f);
    private static readonly Color TitleColor   = Color.white;
    private static readonly Color WinnerColor  = new Color(1f, 0.85f, 0.2f);   // gold
    private static readonly Color NormalColor  = Color.white;
    private static readonly Color PositiveColor= new Color(0.3f, 1f,  0.3f);
    private static readonly Color NegativeColor= new Color(1f,  0.35f,0.35f);
    private static readonly Color BtnPlayAgain = new Color(0.2f, 0.6f, 0.2f);
    private static readonly Color BtnMainMenu  = new Color(0.2f, 0.2f, 0.5f);

    // -- Private fields -----------------------------------------------------
    private Canvas        _canvas;
    private GameObject    _panel;
    private Transform     _rowContainer;
    private SpinnerController _spinner;

    // -- Lifecycle ----------------------------------------------------------
    void Start()
    {
        BuildCanvas();
        _panel.SetActive(false);   // hidden until game over

        _spinner = Object.FindAnyObjectByType<SpinnerController>();

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += Show;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver -= Show;
    }

    // -- Canvas construction -----------------------------------------------
    private void BuildCanvas()
    {
        var canvasGO = new GameObject("ResultsCanvas");
        canvasGO.transform.SetParent(transform, false);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;   // above ScoreTracker (100)

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 600f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 1f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Semi-transparent full-screen backdrop.
        var backdropGO = new GameObject("Backdrop");
        backdropGO.transform.SetParent(canvasGO.transform, false);
        var backdropRT   = backdropGO.AddComponent<RectTransform>();
        backdropRT.anchorMin = Vector2.zero;
        backdropRT.anchorMax = Vector2.one;
        backdropRT.offsetMin = Vector2.zero;
        backdropRT.offsetMax = Vector2.zero;
        var backdropImg  = backdropGO.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.6f);

        // Centred content panel.
        _panel = new GameObject("ResultsPanel");
        _panel.transform.SetParent(canvasGO.transform, false);
        var panelRT     = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta        = new Vector2(PanelW, PanelH);
        var panelImg    = _panel.AddComponent<Image>();
        panelImg.color  = BgColor;

        // Title label.
        var titleGO = MakeLabel("Title", _panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -TitleH), new Vector2(0f, 0f),
            "GAME OVER", 30, TitleColor, TextAnchor.MiddleCenter);
        _ = titleGO;

        // Scrollable row area.
        var rowContainerGO = new GameObject("Rows");
        rowContainerGO.transform.SetParent(_panel.transform, false);
        var rowRT = rowContainerGO.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0f, 1f);
        rowRT.anchorMax        = new Vector2(1f, 1f);
        rowRT.pivot            = new Vector2(0f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, -TitleH - Padding);
        rowRT.sizeDelta        = new Vector2(0f, 0f);
        _rowContainer = rowContainerGO.transform;

        // Buttons row at the bottom.
        AddButton("PlayAgainBtn", _panel.transform,
            new Vector2(0.5f - 0.01f, 0f), new Vector2(-ButtonW * 0.5f - Padding, Padding),
            "Play Again", BtnPlayAgain, OnPlayAgainClicked);

        AddButton("MainMenuBtn", _panel.transform,
            new Vector2(0.5f + 0.01f, 0f), new Vector2(ButtonW * 0.5f + Padding, Padding),
            "Main Menu", BtnMainMenu, OnMainMenuClicked);
    }

    // -- Show results -------------------------------------------------------
    private void Show(List<PlayerState> ranked)
    {
        // Disable spinner input during results screen.
        if (_spinner != null) _spinner.gameObject.SetActive(false);

        // Clear previous result rows.
        foreach (Transform child in _rowContainer) Destroy(child.gameObject);

        bool solitaire = ranked.Count == 1;

        for (int i = 0; i < ranked.Count; i++)
        {
            var p     = ranked[i];
            bool isWinner = (i == 0);
            Color rowColor = isWinner ? WinnerColor : NormalColor;

            string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"#{i + 1}";
            string sign  = p.karmaTotal >= 0 ? "+" : "";
            string label = solitaire
                ? $"{p.playerName}   {sign}{p.karmaTotal} karma   {KarmaRating(p.karmaTotal)}"
                : $"{medal}  {p.playerName}   {sign}{p.karmaTotal} karma";

            Color scoreColor = p.karmaTotal >= 0 ? PositiveColor : NegativeColor;
            if (isWinner) scoreColor = WinnerColor;

            // Each row is a simple label anchored below the previous one.
            float yOffset = -(i * (RowH + 4f));
            var rowGO = MakeLabel($"Row{i}", _rowContainer,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(yOffset, -RowH), new Vector2(0f, 0f),
                label, 20, scoreColor, TextAnchor.MiddleCenter);
            _ = rowGO;
        }

        _panel.SetActive(true);
        Debug.Log("[ResultsScreen] Shown.");
    }

    // -- Button callbacks --------------------------------------------------
    private void OnPlayAgainClicked()
    {
        _panel.SetActive(false);
        if (_spinner != null) _spinner.gameObject.SetActive(true);
        GameManager.Instance?.ResetGame();
        Debug.Log("[ResultsScreen] Play Again.");
    }

    private void OnMainMenuClicked()
    {
        // Placeholder — extend when a main menu scene exists.
        Debug.Log("[ResultsScreen] Main Menu tapped (no scene yet).");
    }

    // -- Karma rating -------------------------------------------------------
    private static string KarmaRating(int total)
    {
        if (total >= RatingEnlightened) return "☯ Enlightened";
        if (total >= RatingNeutral)     return "⚖ Neutral";
        return "⚡ Troubled";
    }

    // -- UI helpers ---------------------------------------------------------
    private static GameObject MakeLabel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        string text, int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var t = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = fontSize;
        t.color     = color;
        t.text      = text;
        t.alignment = alignment;
        return go;
    }

    private static void AddButton(string name, Transform parent,
        Vector2 anchorPivot, Vector2 anchoredPos,
        string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorPivot;
        rt.anchorMax        = anchorPivot;
        rt.pivot            = anchorPivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(ButtonW, ButtonH);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<Text>();
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 18;
        txt.color     = Color.white;
        txt.text      = label;
        txt.alignment = TextAnchor.MiddleCenter;
    }
}

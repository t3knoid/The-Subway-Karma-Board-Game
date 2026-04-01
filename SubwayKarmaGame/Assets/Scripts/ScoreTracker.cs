using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persistent HUD overlay that shows every player's name, karma total,
/// and card-count badge. Updates immediately when a player's karma changes
/// and plays a brief flash animation to highlight the change.
///
/// Built entirely at runtime (no scene YAML required). Attach to any
/// GameObject or let GameBootstrap create it via EnsureScoreTracker().
/// </summary>
public class ScoreTracker : MonoBehaviour
{
    // -- Layout constants ---------------------------------------------------
    private const float PanelWidth     = 220f;
    private const float RowHeight      =  40f;
    private const float Padding        =  10f;
    private const float FontSize       =  18f;
    private const float BadgeFontSize  =  14f;
    private const float FlashDuration  =   0.4f;

    private static readonly Color PanelBg      = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color TextNormal   = Color.white;
    private static readonly Color FlashPositive = new Color(0.3f, 1f, 0.3f);   // green
    private static readonly Color FlashNegative = new Color(1f, 0.3f, 0.3f);   // red

    // -- State --------------------------------------------------------------
    // One entry per registered player.
    private class PlayerRow
    {
        public PlayerState player;
        public Text        scoreText;
        public Text        badgeText;
        public int         lastKarma;
    }

    private readonly List<PlayerRow> _rows   = new List<PlayerRow>();
    private Canvas                   _canvas;
    private RectTransform            _panel;

    // -- Lifecycle ----------------------------------------------------------
    void Start()
    {
        BuildCanvas();

        // Subscribe to future player registrations.
        if (GameManager.Instance != null)
        {
            // Register rows for players already present.
            foreach (var p in GameManager.Instance.Players)
                AddRow(p);

            GameManager.Instance.OnPlayerRegistered += AddRow;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerRegistered -= AddRow;

        foreach (var row in _rows)
            if (row.player != null)
                row.player.OnKarmaChanged -= MakeHandler(row);
    }

    // -- Canvas construction -----------------------------------------------
    private void BuildCanvas()
    {
        var canvasGO = new GameObject("ScoreCanvas");
        canvasGO.transform.SetParent(transform, false);
        DontDestroyOnLoad(canvasGO);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 600f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 1f;   // match height

        canvasGO.AddComponent<GraphicRaycaster>();

        // Translucent panel anchored to top-right corner.
        var panelGO = new GameObject("ScorePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        _panel = panelGO.AddComponent<RectTransform>();
        _panel.anchorMin = new Vector2(1f, 1f);
        _panel.anchorMax = new Vector2(1f, 1f);
        _panel.pivot     = new Vector2(1f, 1f);
        _panel.anchoredPosition = new Vector2(-Padding, -Padding);
        _panel.sizeDelta        = new Vector2(PanelWidth, RowHeight + Padding * 2);

        var bg = panelGO.AddComponent<Image>();
        bg.color = PanelBg;
    }

    // -- Row management ----------------------------------------------------
    private void AddRow(PlayerState player)
    {
        if (player == null) return;

        // Expand panel height to fit new row.
        int rowIndex = _rows.Count;
        float newH = (rowIndex + 1) * RowHeight + Padding * 2;
        _panel.sizeDelta = new Vector2(PanelWidth, newH);

        // Row root.
        var rowGO = new GameObject($"Row_{player.playerName}");
        rowGO.transform.SetParent(_panel, false);

        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0f, 1f);
        rowRT.anchorMax        = new Vector2(1f, 1f);
        rowRT.pivot            = new Vector2(0f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, -(Padding + rowIndex * RowHeight));
        rowRT.sizeDelta        = new Vector2(0f, RowHeight);

        // Score label (left-aligned).
        var scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(rowGO.transform, false);
        var scoreRT  = scoreGO.AddComponent<RectTransform>();
        scoreRT.anchorMin        = Vector2.zero;
        scoreRT.anchorMax        = new Vector2(0.75f, 1f);
        scoreRT.offsetMin        = new Vector2(Padding, 0f);
        scoreRT.offsetMax        = Vector2.zero;
        var scoreText = scoreGO.AddComponent<Text>();
        scoreText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize  = (int)FontSize;
        scoreText.color     = TextNormal;
        scoreText.alignment = TextAnchor.MiddleLeft;

        // Card count badge (right-aligned).
        var badgeGO = new GameObject("BadgeText");
        badgeGO.transform.SetParent(rowGO.transform, false);
        var badgeRT  = badgeGO.AddComponent<RectTransform>();
        badgeRT.anchorMin  = new Vector2(0.75f, 0f);
        badgeRT.anchorMax  = Vector2.one;
        badgeRT.offsetMin  = Vector2.zero;
        badgeRT.offsetMax  = new Vector2(-Padding, 0f);
        var badgeText = badgeGO.AddComponent<Text>();
        badgeText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        badgeText.fontSize  = (int)BadgeFontSize;
        badgeText.color     = new Color(1f, 1f, 0.6f);
        badgeText.alignment = TextAnchor.MiddleRight;

        var row = new PlayerRow
        {
            player    = player,
            scoreText = scoreText,
            badgeText = badgeText,
            lastKarma = player.karmaTotal
        };
        _rows.Add(row);

        RefreshRow(row);

        // Subscribe so the row stays live.
        System.Action<int> handler = MakeHandler(row);
        player.OnKarmaChanged += handler;
    }

    // -- Refresh -----------------------------------------------------------
    private void RefreshRow(PlayerRow row)
    {
        int total = row.player.karmaTotal;
        string sign = total >= 0 ? "+" : "";
        row.scoreText.text = $"{row.player.playerName}: {sign}{total}";
        row.badgeText.text = $"{row.player.hand.Count} cards";
    }

    // -- Flash animation ---------------------------------------------------
    private System.Action<int> MakeHandler(PlayerRow row)
    {
        return newTotal =>
        {
            int delta = newTotal - row.lastKarma;
            row.lastKarma = newTotal;
            RefreshRow(row);
            StartCoroutine(Flash(row.scoreText, delta >= 0 ? FlashPositive : FlashNegative));
        };
    }

    private IEnumerator Flash(Text label, Color flashColor)
    {
        float elapsed = 0f;
        while (elapsed < FlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FlashDuration;
            label.color = Color.Lerp(flashColor, TextNormal, t);
            yield return null;
        }
        label.color = TextNormal;
    }
}

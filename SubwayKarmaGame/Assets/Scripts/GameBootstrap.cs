using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bootstraps all game objects at startup so nothing depends on hand-crafted
/// scene YAML. Uses RuntimeInitializeOnLoadMethod so this runs even if the
/// Board/BoardManager/BoardSetup GameObjects are missing from the scene.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureCamera();
        EnsureBoardManager();
        EnsureBoard();
        EnsureBoardSetup();
        EnsureSpinner();
        EnsureKarmaDeck();
        EnsureKarmaDeckView();
        EnsureGameManager();
        EnsureTurnController();
        EnsureScoreTracker();
        EnsureResultsScreen();
        EnsureGameSetupScreen();
        EnsureEventSystem();
    }

    /// <summary>
    /// The whole game assumes the camera from MainGame.unity: orthographic, size 5, at
    /// (0, 0, -10). Because everything else is created here, pressing Play with a different
    /// scene open (e.g. a new, untitled one with a perspective camera) almost works, but
    /// screen-to-board conversions such as dragging the spinner silently break. Fix it up.
    /// </summary>
    private static void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            Debug.LogWarning("[GameBootstrap] No Main Camera found; created one.");
        }

        var expectedPos = new Vector3(0f, 0f, -10f);
        bool ok = cam.orthographic && Mathf.Approximately(cam.orthographicSize, 5f) &&
                  cam.transform.position == expectedPos && cam.transform.rotation == Quaternion.identity;
        if (ok) return;

        Debug.LogWarning($"[GameBootstrap] Camera '{cam.name}' isn't the game camera " +
                         $"(orthographic={cam.orthographic}, position={cam.transform.position}); fixing it. " +
                         "Open Assets/Scenes/MainGame.unity before pressing Play to use the real scene.");
        cam.usePhysicalProperties = false;
        cam.orthographic          = true;
        cam.orthographicSize      = 5f;
        cam.transform.SetPositionAndRotation(expectedPos, Quaternion.identity);
        cam.clearFlags            = CameraClearFlags.SolidColor;
        cam.backgroundColor       = Color.black;
        cam.nearClipPlane         = 0.3f;
        cam.farClipPlane          = 1000f;
    }

    private static void EnsureBoardManager()
    {
        if (BoardManager.Instance != null) return;

        var go = new GameObject("BoardManager");
        go.AddComponent<BoardManager>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[GameBootstrap] Created BoardManager");
    }

    private static void EnsureBoard()
    {
        // If there's already a BoardDisplay in the scene, leave it alone.
        if (Object.FindAnyObjectByType<BoardDisplay>() != null) return;

        var go = new GameObject("Board");
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoardDisplay>();
        Debug.Log("[GameBootstrap] Created Board with BoardDisplay");
    }

    private static void EnsureBoardSetup()
    {
        if (Object.FindAnyObjectByType<BoardSetup>() != null) return;

        var go = new GameObject("BoardSetup");
        go.AddComponent<BoardSetup>();
        Debug.Log("[GameBootstrap] Created BoardSetup");
    }

    private static void EnsureSpinner()
    {
        if (Object.FindAnyObjectByType<SpinnerController>() != null) return;

        var go = new GameObject("Spinner");
        go.AddComponent<SpinnerController>();
        Debug.Log("[GameBootstrap] Created Spinner");
    }

    private static void EnsureKarmaDeckView()
    {
        if (KarmaDeckView.Instance != null) return;

        // Parents itself under the board the first time it deals.
        new GameObject("KarmaDeckView").AddComponent<KarmaDeckView>();
        Debug.Log("[GameBootstrap] Created KarmaDeckView");
    }

    private static void EnsureKarmaDeck()
    {
        if (KarmaDeck.Instance != null) return;

        var go = new GameObject("KarmaDeck");
        go.AddComponent<KarmaDeck>();
        Debug.Log("[GameBootstrap] Created KarmaDeck");
    }

    private static void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        Debug.Log("[GameBootstrap] Created GameManager");
    }

    private static void EnsureTurnController()
    {
        if (TurnController.Instance != null) return;

        var go = new GameObject("TurnController");
        go.AddComponent<TurnController>();
        Debug.Log("[GameBootstrap] Created TurnController");
    }

    private static void EnsureScoreTracker()
    {
        if (Object.FindAnyObjectByType<ScoreTracker>() != null) return;

        var go = new GameObject("ScoreTracker");
        go.AddComponent<ScoreTracker>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[GameBootstrap] Created ScoreTracker");
    }

    private static void EnsureResultsScreen()
    {
        if (Object.FindAnyObjectByType<ResultsScreen>() != null) return;

        var go = new GameObject("ResultsScreen");
        go.AddComponent<ResultsScreen>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[GameBootstrap] Created ResultsScreen");
    }

    private static void EnsureGameSetupScreen()
    {
        if (Object.FindAnyObjectByType<GameSetupScreen>() != null) return;

        var go = new GameObject("GameSetupScreen");
        go.AddComponent<GameSetupScreen>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[GameBootstrap] Created GameSetupScreen");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[GameBootstrap] Created EventSystem");
    }
}

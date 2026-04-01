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
        EnsureBoardManager();
        EnsureBoard();
        EnsureBoardSetup();
        EnsureSpinner();
        EnsureKarmaDeck();
        EnsurePlayerState();
        EnsureGameManager();
        EnsureScoreTracker();
        EnsureResultsScreen();
        EnsureGameSetupScreen();
        EnsureEventSystem();
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

    private static void EnsureKarmaDeck()
    {
        if (KarmaDeck.Instance != null) return;

        var go = new GameObject("KarmaDeck");
        go.AddComponent<KarmaDeck>();
        Debug.Log("[GameBootstrap] Created KarmaDeck");
    }

    private static void EnsurePlayerState()
    {
        if (PlayerState.Instance != null) return;

        var go = new GameObject("PlayerState");
        go.AddComponent<PlayerState>();
        Debug.Log("[GameBootstrap] Created PlayerState");
    }

    private static void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        Debug.Log("[GameBootstrap] Created GameManager");

        // Register the solitaire player once both singletons are ready.
        if (PlayerState.Instance != null)
            GameManager.Instance.RegisterPlayer(PlayerState.Instance);
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

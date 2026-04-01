using UnityEngine;

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
        if (Object.FindFirstObjectByType<BoardDisplay>() != null) return;

        var go = new GameObject("Board");
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoardDisplay>();
        Debug.Log("[GameBootstrap] Created Board with BoardDisplay");
    }

    private static void EnsureBoardSetup()
    {
        if (Object.FindFirstObjectByType<BoardSetup>() != null) return;

        var go = new GameObject("BoardSetup");
        go.AddComponent<BoardSetup>();
        Debug.Log("[GameBootstrap] Created BoardSetup");
    }
}

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// A 3D-looking subway car token rendered on top of the 2D board.
///
/// Built entirely from Unity primitive Cubes — no external model files.
/// The car is tilted -22° on X so the top face is partially visible in the
/// orthographic view, creating a pseudo-3D appearance.
///
/// Each player slot gets a distinct color and a small radial offset so tokens
/// on the same square do not perfectly overlap.
///
/// Usage:
///   var token = PlayerToken.Create(slotIndex, boardIndex);
///   token.SnapTo(boardIndex);
///   token.MoveTo(targetIndex, () => Debug.Log("arrived"));
/// </summary>
public class PlayerToken : MonoBehaviour
{
    // -- Slot colors (MTA-inspired) -----------------------------------------
    private static readonly Color[] SlotColors = new Color[]
    {
        new Color(0.00f, 0.427f, 0.706f),  // slot 0 — MTA Blue  #006DB4
        new Color(0.769f, 0.118f, 0.227f), // slot 1 — Crimson   #C41E3A
        new Color(0.20f, 0.75f,  0.20f),   // slot 2 — Lime Green
        new Color(0.85f, 0.70f,  0.10f),   // slot 3 — Gold
    };

    // -- Per-slot world-space offsets (radius 0.09 at 90° increments) ------
    private static readonly Vector3[] SlotOffsets = new Vector3[]
    {
        new Vector3( 0.09f,  0.00f, 0f),   // slot 0 — right
        new Vector3( 0.00f,  0.09f, 0f),   // slot 1 — up
        new Vector3(-0.09f,  0.00f, 0f),   // slot 2 — left
        new Vector3( 0.00f, -0.09f, 0f),   // slot 3 — down
    };

    // -- Board segment direction table (Z rotation to face direction) ------
    // The board is 22 squares: 0-5 top (→), 6-10 right (↓), 11-16 bottom (←), 17-21 left (↑)
    private static float DirectionZ(int boardIndex)
    {
        if (boardIndex <= 5)  return   0f;   // top row:    facing right
        if (boardIndex <= 10) return -90f;   // right col:  facing down
        if (boardIndex <= 16) return 180f;   // bottom row: facing left
        return                          90f; // left col:   facing up
    }

    // -- Car geometry constants (world units) ------------------------------
    private const float BodyW   = 0.40f;  // width  (X)
    private const float BodyH   = 0.12f;  // height (Y)
    private const float BodyD   = 0.17f;  // depth  (Z)
    private const float TiltX   = -22f;   // X tilt to show top face
    private const float TokenZ  = -0.5f;  // render above board sprites (Z<0 = toward camera)
    private const float MoveSpeed = 0.18f; // seconds per square

    // -- State -------------------------------------------------------------
    private int   _slot;
    private Color _color;
    private int   _boardIndex;   // square the token is on (or heading to while moving)
    private bool  _moving;

    // Tap-zone transforms, looked up once (zones live for the whole session).
    private static readonly Transform[] ZoneCache = new Transform[BoardManager.BoardSize];

    // -- Lifecycle ---------------------------------------------------------

    void OnEnable()  => BoardDisplay.OnBoardFitted += HandleBoardFitted;
    void OnDisable() => BoardDisplay.OnBoardFitted -= HandleBoardFitted;

    /// <summary>Screen rotated or resized: the board moved, so follow it.</summary>
    private void HandleBoardFitted()
    {
        if (!_moving) SnapTo(_boardIndex);   // a move in progress re-targets every frame
    }

    // -- Factory -----------------------------------------------------------

    /// <summary>Creates and returns a token for the given player slot at boardIndex.</summary>
    public static PlayerToken Create(int slot, int boardIndex)
    {
        EnsureDirectionalLight();

        int safeSlot = Mathf.Clamp(slot, 0, SlotColors.Length - 1);
        Color color = SlotColors[safeSlot];

        var root = new GameObject($"PlayerToken_Slot{safeSlot}");
        DontDestroyOnLoad(root);

        var token = root.AddComponent<PlayerToken>();
        token._slot  = safeSlot;
        token._color = color;

        BuildCar(root.transform, color);
        token.SnapTo(boardIndex);
        return token;
    }

    // -- Public API --------------------------------------------------------

    /// <summary>Instantly moves the token to boardIndex.</summary>
    public void SnapTo(int boardIndex)
    {
        _boardIndex = boardIndex;
        transform.position = SquareWorldPos(boardIndex);
        transform.eulerAngles = new Vector3(TiltX, 0f, DirectionZ(boardIndex));
    }

    /// <summary>
    /// Animates the token square-by-square from its current board position
    /// to targetIndex. Calls onComplete when done.
    /// </summary>
    public void MoveTo(int targetIndex, Action onComplete)
    {
        StartCoroutine(MoveCoroutine(targetIndex, onComplete));
    }

    // -- Coroutine ---------------------------------------------------------

    private IEnumerator MoveCoroutine(int target, Action onComplete)
    {
        // Callers (TurnController) move one square at a time.
        // Endpoints are recomputed each frame from the squares, so a screen
        // rotation or resize mid-move still lands on the right square.
        int   from       = _boardIndex;
        float targetZRot = DirectionZ(target);
        float startZRot  = transform.eulerAngles.z;
        float elapsed    = 0f;

        _moving     = true;
        _boardIndex = target;

        while (elapsed < MoveSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / MoveSpeed));
            transform.position = Vector3.Lerp(SquareWorldPos(from), SquareWorldPos(target), t);
            // Lerp Z rotation (handle wrap-around via delta)
            float zDelta = Mathf.DeltaAngle(startZRot, targetZRot);
            transform.eulerAngles = new Vector3(TiltX, 0f, startZRot + zDelta * t);
            yield return null;
        }

        transform.position     = SquareWorldPos(target);
        transform.eulerAngles  = new Vector3(TiltX, 0f, targetZRot);
        _moving = false;
        onComplete?.Invoke();
    }

    // -- Geometry helpers --------------------------------------------------

    private Vector3 SquareWorldPos(int boardIndex)
    {
        int i = ((boardIndex % BoardManager.BoardSize) + BoardManager.BoardSize) % BoardManager.BoardSize;
        if (ZoneCache[i] == null)
        {
            var found = GameObject.Find($"TapZone_{i:D2}");
            if (found == null) return Vector3.zero;
            ZoneCache[i] = found.transform;
        }
        var zone = ZoneCache[i];

        int safeSlot = Mathf.Clamp(_slot, 0, SlotOffsets.Length - 1);
        return zone.transform.position
               + SlotOffsets[safeSlot]
               + new Vector3(0f, 0f, TokenZ);
    }

    // -- Car construction --------------------------------------------------

    private static void BuildCar(Transform root, Color color)
    {
        // Apply global tilt to root; children inherit it.
        root.localEulerAngles = new Vector3(TiltX, 0f, 0f);

        Color roofColor    = Color.Lerp(color, Color.white, 0.35f);
        Color bumperColor  = Color.Lerp(color, Color.black, 0.25f);
        Color windowColor  = new Color(0.15f, 0.15f, 0.20f);
        Color lightColor   = new Color(1.00f, 0.95f, 0.70f);

        // Body
        AddCube(root, "Body",    color,    new Vector3(0f, 0f, 0f),
                                           new Vector3(BodyW, BodyH, BodyD));

        // Roof (slightly narrower, sits on top)
        AddCube(root, "Roof",    roofColor, new Vector3(0f, BodyH * 0.5f + 0.018f, 0f),
                                            new Vector3(BodyW * 0.90f, 0.036f, BodyD * 0.85f));

        // Front bumper
        AddCube(root, "BumperF", bumperColor, new Vector3(BodyW * 0.5f + 0.008f, 0f, 0f),
                                              new Vector3(0.016f, BodyH * 0.80f, BodyD * 0.90f));

        // Rear bumper
        AddCube(root, "BumperR", bumperColor, new Vector3(-(BodyW * 0.5f + 0.008f), 0f, 0f),
                                              new Vector3(0.016f, BodyH * 0.80f, BodyD * 0.90f));

        // Window strip — near side (positive Z)
        AddCube(root, "WinNear", windowColor, new Vector3(0f, BodyH * 0.18f, BodyD * 0.5f + 0.002f),
                                              new Vector3(BodyW * 0.72f, BodyH * 0.45f, 0.004f));

        // Window strip — far side (negative Z)
        AddCube(root, "WinFar",  windowColor, new Vector3(0f, BodyH * 0.18f, -(BodyD * 0.5f + 0.002f)),
                                              new Vector3(BodyW * 0.72f, BodyH * 0.45f, 0.004f));

        // Headlights (front corners)
        float hlY = -BodyH * 0.25f;
        float hlZ =  BodyD * 0.38f;
        float hlX =  BodyW * 0.42f;
        AddCube(root, "HL_L",  lightColor, new Vector3(hlX, hlY,  hlZ), new Vector3(0.022f, 0.022f, 0.022f));
        AddCube(root, "HL_R",  lightColor, new Vector3(hlX, hlY, -hlZ), new Vector3(0.022f, 0.022f, 0.022f));
    }

    private static void AddCube(Transform parent, string name, Color color,
                                 Vector3 localPos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;

        // Remove box collider — tokens are visual only.
        var col = go.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.Destroy(col);

        // Copy the primitive's built-in default material instead of using
        // Shader.Find("Standard"): Shader.Find returns null in player builds
        // when no asset references the shader, so it only worked in the Editor.
        var mr  = go.GetComponent<MeshRenderer>();
        var mat = new Material(mr.sharedMaterial);
        mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.08f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0.00f);
        mr.material = mat;
    }

    // -- Directional light -------------------------------------------------

    private static void EnsureDirectionalLight()
    {
        // If a directional light already exists in the scene, leave it.
        var existing = UnityEngine.Object.FindAnyObjectByType<Light>();
        if (existing != null && existing.type == LightType.Directional) return;

        var go    = new GameObject("TokenDirectionalLight");
        DontDestroyOnLoad(go);
        var light = go.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1.2f;
        light.color     = Color.white;
        go.transform.eulerAngles = new Vector3(50f, 40f, 0f);

        Debug.Log("[PlayerToken] Created directional light for 3D token shading.");
    }
}

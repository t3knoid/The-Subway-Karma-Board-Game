using UnityEngine;

/// <summary>
/// Creates the 22 invisible tap-zone GameObjects at runtime and parents them
/// to the Board sprite so they scale automatically with any FitToCamera result.
///
/// Positions are in Board LOCAL SPACE (pixel offset from image centre ÷ PPU):
///   local_x = (pixel_x − 1205.5) / 100
///   local_y = (1417.5  − pixel_y) / 100   ← Y flipped: image-down = local-down
///
/// Source measurements on the cropped 2411×2835 board.jpg:
///   Image centre      = (1205.5, 1417.5) px
///   Corner size       = 391 × 392 px  →  local 3.91 × 3.92
///   Top/bottom square = 405.5 × 392 px  →  local 4.06 × 3.92
///   Left/right square = 391 × 406.2 px  →  local 3.91 × 4.06
///
/// Board layout: 22 squares clockwise — 6 top, 5 right, 6 bottom, 5 left.
/// Stations (corners) at indices 0, 5, 11, 16.
/// NoOp (blank) squares at indices 2, 7, 13, 18.
/// </summary>
public class BoardSetup : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // LOCAL-SPACE square centres (image pixel offset ÷ PPU, Y-flipped).
    // These are in the Board sprite's own coordinate system, so they scale
    // automatically whenever the Board's transform changes (e.g. FitToCamera).
    // -------------------------------------------------------------------------
    private static readonly Vector3[] SquareCenters = new Vector3[22]
    {
        // ── Top row: left → right (indices 0–5) ─ local_y = (1417.5−206)/100 = +12.12
        new Vector3(-10.06f,  12.12f, 0f),  // 0  Station:       Broadway / 7 Ave Local (1) — 96th St
        new Vector3( -6.08f,  12.12f, 0f),  // 1  EarlyTrain:    Early Train — Move 1 Space Ahead
        new Vector3( -2.02f,  12.12f, 0f),  // 2  NoOp:          Empty
        new Vector3(  2.03f,  12.12f, 0f),  // 3  LateTrain:     Late Train — Lose a Turn
        new Vector3(  6.09f,  12.12f, 0f),  // 4  ExpressTrain:  6 Avenue Express (F)
        new Vector3( 10.07f,  12.12f, 0f),  // 5  Station:       6 Avenue Local / 47-50th Streets (F)

        // ── Right column: top → bottom (indices 6–10) ─ local_x = (2212.5−1205.5)/100 = +10.07
        new Vector3( 10.07f,   8.12f, 0f),  // 6  SickPassenger: Sick Passenger — Go Back 1 Space
        new Vector3( 10.07f,   4.06f, 0f),  // 7  NoOp:          Empty
        new Vector3( 10.07f,   0.00f, 0f),  // 8  EarlyTrain:    Early Train — Move 1 Space Ahead
        new Vector3( 10.07f,  -4.06f, 0f),  // 9  LateTrain:     Late Train — Lose a Turn
        new Vector3( 10.07f,  -8.12f, 0f),  // 10 ExpressTrain:  8 Avenue Express (A)

        // ── Bottom row: right → left (indices 11–16) ─ local_y = (1417.5−2629)/100 = −12.12
        new Vector3( 10.07f, -12.12f, 0f),  // 11 Station:       Times Square / Eighth Avenue Local (C)
        new Vector3(  6.09f, -12.12f, 0f),  // 12 EarlyTrain:    Early Train — Move 1 Space Ahead
        new Vector3(  2.03f, -12.12f, 0f),  // 13 NoOp:          Empty
        new Vector3( -2.02f, -12.12f, 0f),  // 14 LateTrain:     Late Train — Lose a Turn
        new Vector3( -6.08f, -12.12f, 0f),  // 15 ExpressTrain:  Broadway Express (Q)
        new Vector3(-10.06f, -12.12f, 0f),  // 16 Station:       Astoria Blvd / Broadway Local (N)

        // ── Left column: bottom → top (indices 17–21) ─ local_x = (199.5−1205.5)/100 = −10.06
        new Vector3(-10.06f,  -8.12f, 0f),  // 17 SickPassenger: Sick Passenger — Go Back 1 Space
        new Vector3(-10.06f,  -4.06f, 0f),  // 18 NoOp:          Empty
        new Vector3(-10.06f,   0.00f, 0f),  // 19 EarlyTrain:    Early Train — Move 1 Space Ahead
        new Vector3(-10.06f,   4.06f, 0f),  // 20 LateTrain:     Late Train — Lose a Turn
        new Vector3(-10.06f,   8.12f, 0f),  // 21 ExpressTrain:  Seventh Avenue Express (2)
    };

    // Collider sizes in Board LOCAL SPACE (pixel measurements ÷ PPU).
    // World size = local size × Board transform scale (set by FitToCamera).
    private static readonly Vector2 TopBottomSize = new Vector2(4.06f, 3.92f); // top/bottom non-corner (405.5×392 px)
    private static readonly Vector2 LeftRightSize = new Vector2(3.91f, 4.06f); // left/right non-corner (391×406.2 px)
    private static readonly Vector2 CornerSize    = new Vector2(3.91f, 3.92f); // corner Station squares (391×392 px)

    // Station indices (corners): 0, 5, 11, 16
    private static readonly int[] StationIndices = { 0, 5, 11, 16 };

    void Awake()
    {
        CreateTapZones();
    }

    private void CreateTapZones()
    {
        // Parent zones to the Board sprite so they scale automatically with FitToCamera.
        var boardDisplay = FindAnyObjectByType<BoardDisplay>();
        Transform boardRoot = boardDisplay != null ? boardDisplay.transform : transform;

        for (int i = 0; i < BoardManager.BoardSize; i++)
        {
            GameObject zone = new GameObject($"TapZone_{i:D2}");
            // worldPositionStays=false → use localPosition directly in Board's local space.
            zone.transform.SetParent(boardRoot, false);
            zone.transform.localPosition = SquareCenters[i];
            zone.layer = LayerMask.NameToLayer("Default");

            // Invisible sprite renderer for flash feedback
            SpriteRenderer sr = zone.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, 0f); // fully transparent by default
            sr.sortingOrder = 1;

            BoxCollider2D col = zone.AddComponent<BoxCollider2D>();
            col.size = GetColliderSize(i);

            SquareTapHandler handler = zone.AddComponent<SquareTapHandler>();
            handler.squareIndex = i;
        }
    }

    private Vector2 GetColliderSize(int index)
    {
        // Corner stations
        foreach (int si in StationIndices)
            if (index == si) return CornerSize;

        // Right column (6–10)
        if (index >= 6 && index <= 10) return LeftRightSize;

        // Left column (17–21)
        if (index >= 17 && index <= 21) return LeftRightSize;

        // Top and bottom rows (1–4, 12–15)
        return TopBottomSize;
    }
}

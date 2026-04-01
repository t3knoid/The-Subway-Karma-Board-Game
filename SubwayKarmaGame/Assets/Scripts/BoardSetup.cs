using UnityEngine;

/// <summary>
/// Creates the 22 invisible tap-zone GameObjects at runtime and overlays them
/// on the board sprite. Positions are expressed in world space and were
/// derived from pixel measurements on the cropped 2411×2835 board.jpg.
///
/// Derivation (camera orthoSize=5, aspect=960/600=1.6, PPU=100):
///   board scale  = 10 / (2835/100) = 0.3527
///   px-to-world  = 0.003527 wu/px
///   board world  = 8.50 wide × 10.00 tall
///   image center = (1205.5, 1417.5) px → world (0, 0)
///
/// Square pixel sizes:
///   Corner (measured):        391 × 392 px  →  1.38 × 1.38 wu
///   Top/bottom non-corner:    405.5 × 392 px  →  1.43 × 1.38 wu
///   Left/right non-corner:    391 × 406.2 px  →  1.38 × 1.43 wu
///
/// Board layout: 22 squares clockwise — 6 on top, 5 on right, 6 on bottom, 5 on left.
/// Stations (corners) at indices 0, 5, 11, 16.
/// NoOp (blank) squares at indices 2, 7, 13, 18.
/// </summary>
public class BoardSetup : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // World-space centers computed from pixel measurements on 2411×2835 board.jpg.
    // px-to-world = 0.003527; worldX = (px - 1205.5)*ptw; worldY = (1417.5 - py)*ptw
    // -------------------------------------------------------------------------
    private static readonly Vector3[] SquareCenters = new Vector3[22]
    {
        // ── Top row: left → right (indices 0–5) ────────────────────────
        new Vector3(-3.55f,  4.27f, 0f),  // 0  Station:       Broadway / 7 Ave Local (1) — 96th St
        new Vector3(-2.14f,  4.27f, 0f),  // 1  EarlyTrain:    Early Train — Move 1 Space Ahead
        new Vector3(-0.71f,  4.27f, 0f),  // 2  NoOp:          Empty
        new Vector3( 0.72f,  4.27f, 0f),  // 3  LateTrain:     Late Train — Lose a Turn
        new Vector3( 2.15f,  4.27f, 0f),  // 4  ExpressTrain:  6 Avenue Express (F)
        new Vector3( 3.55f,  4.27f, 0f),  // 5  Station:       6 Avenue Local / 47-50th Streets (F)

        // ── Right column: top → bottom (indices 6–10) ──────────────────
        new Vector3( 3.55f,  2.87f, 0f),  // 6  SickPassenger: Sick Passenger — Go Back 1 Space
        new Vector3( 3.55f,  1.43f, 0f),  // 7  NoOp:          Empty
        new Vector3( 3.55f,  0.00f, 0f),  // 8  EarlyTrain:    Early Train — Move 1 Space Ahead
        new Vector3( 3.55f, -1.43f, 0f),  // 9  LateTrain:     Late Train — Lose a Turn
        new Vector3( 3.55f, -2.87f, 0f),  // 10 ExpressTrain:  8 Avenue Express (A)

        // ── Bottom row: right → left (indices 11–16) ───────────────────
        new Vector3( 3.55f, -4.27f, 0f),  // 11 Station:       Times Square / Eighth Avenue Local (C)
        new Vector3( 2.15f, -4.27f, 0f),  // 12 EarlyTrain:    Early Train — Move 1 Space Ahead
        new Vector3( 0.72f, -4.27f, 0f),  // 13 NoOp:          Empty
        new Vector3(-0.71f, -4.27f, 0f),  // 14 LateTrain:     Late Train — Lose a Turn
        new Vector3(-2.14f, -4.27f, 0f),  // 15 ExpressTrain:  Broadway Express (Q)
        new Vector3(-3.55f, -4.27f, 0f),  // 16 Station:       Astoria Blvd / Broadway Local (N)

        // ── Left column: bottom → top (indices 17–21) ──────────────────
        new Vector3(-3.55f, -2.87f, 0f),  // 17 SickPassenger: Sick Passenger — Go Back 1 Space
        new Vector3(-3.55f, -1.43f, 0f),  // 18 NoOp:          Empty
        new Vector3(-3.55f,  0.00f, 0f),  // 19 EarlyTrain:    Early Train — Move 1 Space Ahead
        new Vector3(-3.55f,  1.43f, 0f),  // 20 LateTrain:     Late Train — Lose a Turn
        new Vector3(-3.55f,  2.87f, 0f),  // 21 ExpressTrain:  Seventh Avenue Express (2)
    };

    // Collider sizes derived from pixel measurements × 0.003527 wu/px
    private static readonly Vector2 TopBottomSize = new Vector2(1.43f, 1.38f); // top/bottom row non-corner (405.5×392 px)
    private static readonly Vector2 LeftRightSize = new Vector2(1.38f, 1.43f); // left/right col non-corner (391×406.2 px)
    private static readonly Vector2 CornerSize    = new Vector2(1.38f, 1.38f); // corner Station squares (391×392 px)

    // Station indices (corners): 0, 5, 11, 16
    private static readonly int[] StationIndices = { 0, 5, 11, 16 };

    void Awake()
    {
        CreateTapZones();
    }

    private void CreateTapZones()
    {
        for (int i = 0; i < BoardManager.BoardSize; i++)
        {
            BoardSquare square = BoardManager.Instance != null
                ? BoardManager.Instance.GetSquare(i)
                : null;

            GameObject zone = new GameObject($"TapZone_{i:D2}");
            zone.transform.SetParent(transform);
            zone.transform.position = SquareCenters[i];
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

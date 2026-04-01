using UnityEngine;

/// <summary>
/// Creates the 20 invisible tap-zone GameObjects at runtime and overlays them
/// on the board sprite. Positions are expressed in world space and were
/// calculated from the 2550×3300 board.jpg at PPU=330.
///
/// To calibrate: open the scene in the Unity Editor, run Play mode, select
/// each TapZone child in the Hierarchy, and adjust its Transform position
/// until the BoxCollider2D gizmo lines up with the corresponding square on
/// the board image.  Exact width/height of each collider may also need
/// adjustment per square type (corner vs. edge).
/// </summary>
public class BoardSetup : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Approximate world-space centers for each of the 20 squares (clockwise).
    // Derived from the 2550×3300 board image at PPU=330 (world size ≈ 7.73×10).
    // -------------------------------------------------------------------------
    private static readonly Vector3[] SquareCenters = new Vector3[20]
    {
        // ── Top row: left → right ──────────────────────────────────────────
        new Vector3(-3.02f,  4.20f, 0f),  // 0  Station:      Broadway / 7 Avenue Local (1)
        new Vector3(-1.81f,  4.20f, 0f),  // 1  EarlyTrain:   96th Street
        new Vector3(-0.61f,  4.20f, 0f),  // 2  EarlyTrain:   Early Train – Move 1 Space Ahead
        new Vector3( 0.60f,  4.20f, 0f),  // 3  LateTrain:    Late Train – Lose a Turn
        new Vector3( 1.81f,  4.20f, 0f),  // 4  ExpressTrain: 6 Avenue Express (F)
        new Vector3( 3.01f,  4.20f, 0f),  // 5  Station:      6 Avenue Local / 47-50th Streets (D)

        // ── Right column: top → bottom ─────────────────────────────────────
        new Vector3( 3.02f,  2.52f, 0f),  // 6  SickPassenger: Sick Passenger – Go Back 1 Space
        new Vector3( 3.02f,  0.84f, 0f),  // 7  EarlyTrain:    Early Train – Move 1 Space Ahead
        new Vector3( 3.02f, -0.84f, 0f),  // 8  LateTrain:     Late Train – Lose a Turn
        new Vector3( 3.02f, -2.52f, 0f),  // 9  ExpressTrain:  8 Avenue Express (A)
        new Vector3( 3.01f, -4.20f, 0f),  // 10 Station:       Times Square / Eighth Avenue Local (C)

        // ── Bottom row: right → left ───────────────────────────────────────
        new Vector3( 1.50f, -4.20f, 0f),  // 11 EarlyTrain:   Early Train – Move 1 Space Ahead
        new Vector3(-0.01f, -4.20f, 0f),  // 12 LateTrain:     Late Train – Lose a Turn
        new Vector3(-1.52f, -4.20f, 0f),  // 13 ExpressTrain:  Broadway Express (Q)
        new Vector3(-3.02f, -4.20f, 0f),  // 14 Station:       Astoria Blvd / Broadway Local (N)

        // ── Left column: bottom → top ──────────────────────────────────────
        new Vector3(-3.02f, -2.80f, 0f),  // 15 SickPassenger: Sick Passenger – Go Back 1 Space
        new Vector3(-3.02f, -1.40f, 0f),  // 16 EarlyTrain:    Early Train – Move 1 Space Ahead
        new Vector3(-3.02f,  0.00f, 0f),  // 17 LateTrain:     Late Train – Lose a Turn
        new Vector3(-3.02f,  1.40f, 0f),  // 18 ExpressTrain:  Seventh Avenue Express (2)
        new Vector3(-3.02f,  2.80f, 0f),  // 19 SickPassenger: Sick Passenger – Go Back 1 Space
    };

    // Collider half-sizes per square role (width, height in world units)
    private static readonly Vector2 TopBottomSize    = new Vector2(1.10f, 1.35f); // top/bottom row
    private static readonly Vector2 LeftRightSize    = new Vector2(1.15f, 1.55f); // left/right column
    private static readonly Vector2 CornerSize       = new Vector2(1.15f, 1.35f); // corner Station squares

    // Station indices (corners)
    private static readonly int[] StationIndices = { 0, 5, 10, 14 };

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

        // Left column (15–19)
        if (index >= 15 && index <= 19) return LeftRightSize;

        // Right column (6–9)
        if (index >= 6 && index <= 9) return LeftRightSize;

        // Top and bottom rows (1–4, 11–13)
        return TopBottomSize;
    }
}

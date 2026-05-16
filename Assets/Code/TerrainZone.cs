using UnityEngine;

/// <summary>
/// Overrides ball friction while the ball is inside this trigger volume.
/// Reset to defaultFriction is handled by BallPhysics.Update() each frame,
/// so no explicit reset is needed here.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TerrainZone : MonoBehaviour
{
    public enum TerrainType { Cesped, Hielo, Arena }

    public TerrainType terrainType = TerrainType.Cesped;
    public BallPhysics ball;

    static float FrictionFor(TerrainType t) => t switch
    {
        TerrainType.Cesped => 0.4f,
        TerrainType.Hielo => 0.1f,
        TerrainType.Arena => 0.6f,
        _ => 0.4f
    };

    void Update()
    {
        if (ball == null) return;

        Vector3 local = transform.InverseTransformPoint(ball.transform.position);
        bool inside = Mathf.Abs(local.x) <= 0.5f &&
                      Mathf.Abs(local.y) <= 0.5f &&
                      Mathf.Abs(local.z) <= 0.5f;

        // Only set; BallPhysics.Update() resets to default each frame first
        if (inside)
            ball.currentFriction = FrictionFor(terrainType);
    }
}

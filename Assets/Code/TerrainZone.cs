using UnityEngine;

/// <summary>
/// Sobreescribe la fricción del PhysicsManager mientras la bola está dentro.
/// Tipos: Césped µ=0.4 | Hielo µ=0.1 | Arena µ=0.6
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TerrainZone : MonoBehaviour
{
    public enum TerrainType { Cesped, Hielo, Arena }

    public TerrainType terrainType = TerrainType.Cesped;
    public BallController ball;

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

        if (inside)
            ball.physics.currentFriction = FrictionFor(terrainType);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = terrainType switch
        {
            TerrainType.Cesped => Color.green,
            TerrainType.Hielo => Color.cyan,
            TerrainType.Arena => Color.yellow,
            _ => Color.white
        };
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
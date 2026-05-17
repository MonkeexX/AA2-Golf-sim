using UnityEngine;

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

        if (inside)
            ball.currentFriction = FrictionFor(terrainType);
    }
}

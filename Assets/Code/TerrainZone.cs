using UnityEngine;

public class TerrainZone : MonoBehaviour
{
    public enum TerrainType { Cesped, Hielo, Arena }

    public TerrainType terrainType = TerrainType.Cesped;

    public BallPhysics ball;
    private BoxCollider box;

    void Start()
    {
        box = GetComponent<BoxCollider>();
    }

    void Update()
    {
        if (ball == null || box == null) return;

        Vector3 local = transform.InverseTransformPoint(ball.transform.position);

        bool inside = Mathf.Abs(local.x) <= 0.5f &&
                      Mathf.Abs(local.y) <= 0.5f &&
                      Mathf.Abs(local.z) <= 0.5f;

        if (inside)
            ball.currentFriction = GetFriction();
        else
            ball.currentFriction = ball.rollingFriction;
    }

    float GetFriction()
    {
        switch (terrainType)
        {
            case TerrainType.Cesped: return 0.4f;
            case TerrainType.Hielo: return 0.1f;
            case TerrainType.Arena: return 0.6f;
            default: return 0.4f;
        }
    }
}

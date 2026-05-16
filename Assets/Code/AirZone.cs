using UnityEngine;

/// <summary>
/// Applies a constant wind force to the ball while it is inside this volume.
/// Works with the air-resistance zone required by Level 2.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class AirZone : MonoBehaviour
{
    public BallPhysics ball;

    [Header("Wind")]
    public Vector3 windDirection = Vector3.forward;
    public float windStrength = 20f;

    void Update()
    {
        if (ball == null) return;

        Vector3 local = transform.InverseTransformPoint(ball.transform.position);
        bool inside = Mathf.Abs(local.x) <= 0.5f &&
                      Mathf.Abs(local.y) <= 0.5f &&
                      Mathf.Abs(local.z) <= 0.5f;

        if (inside)
            ball.AddForce(windDirection.normalized * windStrength);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, windDirection.normalized * 3f);
    }
}

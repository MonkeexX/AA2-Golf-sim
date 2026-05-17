using UnityEngine;

/// <summary>
/// Aplica fuerza de viento constante a la bola mientras está dentro.
/// Zona de resistencia del aire para Nivel 2.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class AirZone : MonoBehaviour
{
    public BallController ball;

    [Header("Viento")]
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
            ball.physics.AddForce(windDirection.normalized * windStrength);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        Gizmos.DrawRay(transform.position, windDirection.normalized * 3f);
    }
}

using UnityEngine;

public class AirZone : MonoBehaviour
{
    public BallPhysics ball;

    [Header("Air Direction")]
    public Vector3 windDirection = Vector3.forward;

    [Header("Air Force")]
    public float windStrength = 20f;

    private BoxCollider box;

    void Start()
    {
        box = GetComponent<BoxCollider>();
    }

    void Update()
    {
        if (ball == null) return;

        Vector3 center = transform.position;
        Vector3 halfSize = Vector3.Scale(transform.lossyScale, Vector3.one) * 0.5f;

        Vector3 local = transform.InverseTransformPoint(ball.transform.position);

        bool inside = Mathf.Abs(local.x) <= 0.5f &&
                      Mathf.Abs(local.y) <= 0.5f &&
                      Mathf.Abs(local.z) <= 0.5f;

        if (inside)
        {
            Vector3 force = windDirection.normalized * windStrength;
            ball.AddForce(force);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(
            transform.position,
            windDirection.normalized * 3f
        );
    }
}

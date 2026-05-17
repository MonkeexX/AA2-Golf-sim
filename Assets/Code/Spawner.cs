using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ResetZone : MonoBehaviour
{
    [Header("Referencies")]
    public BallController ball;
    public Transform spawnPoint;

    void Update()
    {
        if (ball == null || spawnPoint == null) return;

        Vector3 local = transform.InverseTransformPoint(ball.transform.position);
        bool inside = Mathf.Abs(local.x) <= 0.5f &&
                        Mathf.Abs(local.y) <= 0.5f &&
                        Mathf.Abs(local.z) <= 0.5f;

        if (inside)
        {
            ball.ResetToPosition(spawnPoint.position);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}

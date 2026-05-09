using UnityEngine;

public class BallController : MonoBehaviour
{

    public BallPhysics ball;

    public float forcePower = 10f;

    void Update()
    {
        Debug.Log("CLICK DETECTADO");

        if (Input.GetMouseButtonDown(0))
        {
            ApplyForce();
        }
    }
    void ApplyForce()
    {
        Vector3 dir = Vector3.forward;

        ball.velocity += dir.normalized * forcePower;
    }
}

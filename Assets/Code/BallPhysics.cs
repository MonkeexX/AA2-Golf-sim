using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    public float mass = 1f;
    public Vector3 velocity;
    public Vector3 acceleration;

    public float rollingFriction = 0.2f;
    public float airDrag = 0.05f;
    public float forcePower = 100;

    private Vector3 dir;

    void Update()
    {
        float dt = Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            ApplyImpulse();
        }

        ApplyForces();

        velocity += acceleration * dt;

        ApplyVelocityDamping(dt);

        MoveWithCollisions(dt);

        HandleCollisions();

        acceleration = Vector3.zero;

    }

    void ApplyImpulse()
    {
        dir = Camera.main.transform.forward;

        velocity += dir.normalized * forcePower;
    }

    void MoveWithCollisions(float dt)
    {
        float radius = 0.5f;
        float remainingDistance = velocity.magnitude * dt;
        Vector3 direction = velocity.normalized;

        int maxBounces = 3;

        while (remainingDistance > 0.001f && maxBounces-- > 0)
        {
            RaycastHit hit;

            if (Physics.SphereCast(transform.position, radius, direction, out hit, remainingDistance))
            {
                transform.position = hit.point + hit.normal * radius;

                velocity = Vector3.Reflect(velocity, hit.normal) * 0.6f;

                direction = velocity.normalized;
                remainingDistance -= hit.distance;
            }
            else
            {
                transform.position += direction * remainingDistance;
                break;
            }
        }
    }

    void ApplyForces()
    {
        acceleration += Physics.gravity;

        // Fricción de rodadura
        if (velocity.magnitude > 0.01f)
        {
            Vector3 friction = -velocity.normalized * rollingFriction;
            acceleration += (friction / mass);
        }

        // Resistencia del aire
        Vector3 drag = -velocity * airDrag;
        acceleration += drag / mass;
    }

    public void AddForce(Vector3 force)
    {
        acceleration += force / mass;
    }

    void ApplyVelocityDamping(float dt)
    {
        // Fricción continua (muy importante)
        float speed = velocity.magnitude;

        if (speed > 0.001f)
        {
            float frictionFactor = 1f - (rollingFriction * dt);

            if (frictionFactor < 0f)
                frictionFactor = 0f;

            velocity *= frictionFactor;
        }
    }

    void HandleCollisions()
    {
        float radius = 0.5f;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider col in hits)
        {
            if (col.attachedRigidbody == null || col.attachedRigidbody.isKinematic)
            {
                Vector3 closest = col.ClosestPoint(transform.position);
                Vector3 dir = transform.position - closest;

                float dist = dir.magnitude;

                if (dist == 0f) continue;

                float penetration = radius - dist;

                if (penetration > 0f)
                {
                    Vector3 normal = dir.normalized;

                    // Sacar la bola fuera de la colisión
                    transform.position += normal * penetration;

                    // Si la velocidad va hacia dentro, reflejar
                    float vDot = Vector3.Dot(velocity, normal);
                    if (vDot < 0f)
                    {
                        velocity -= normal * vDot * 1.5f; // rebote suave
                    }
                }
            }
        }
    }
}

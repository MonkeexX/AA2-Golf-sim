using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    public float mass = 1f;
    public Vector3 velocity;
    public Vector3 acceleration;

    public float rollingFriction = 0.2f;
    public float airDrag = 0.05f;
    public float forcePower = 100;
    public float maxForcePower = 300f;
    public float chargeRate = 200f;
    public LineRenderer lineRenderer;
    public int trajectorySteps = 30;
    public float simulationStep = 0.1f;

    [Header("Air Resistance (F = 0.5 * rho * v^2 * Cd * A)")]
    public float airDensity = 1.225f;   // rho (kg/m³)
    public float dragCoeff = 0.47f;    // Cd  (esfera)
    public float ballRadius = 0.5f;     // r   (m)

    private float crossSectionArea;     // ? * r²
    private float currentForce;
    private bool isCharging;
    private Vector3 dir;

    void Awake()
    {
        crossSectionArea = Mathf.PI * ballRadius * ballRadius;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currentForce = 0f;
        }

        if (Input.GetMouseButton(0) && isCharging)
        {
            currentForce += chargeRate * dt;
            currentForce = Mathf.Clamp(currentForce, 0f, maxForcePower);
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            ApplyImpulse(currentForce);
            isCharging = false;
            currentForce = 0f;
        }

        if (isCharging)
            DrawTrajectory();
        else if (lineRenderer != null)
            lineRenderer.positionCount = 0;

        ApplyForces();

        velocity += acceleration * dt;

        ApplyVelocityDamping(dt);

        MoveWithCollisions(dt);

        HandleCollisions();

        acceleration = Vector3.zero;
    }

    void DrawTrajectory()
    {
        if (lineRenderer == null || !isCharging) return;

        Vector3 pos = transform.position;
        Vector3 vel = GetPredictedInitialVelocity();
        Vector3[] points = new Vector3[trajectorySteps];

        for (int i = 0; i < trajectorySteps; i++)
        {
            points[i] = pos;

            vel += Physics.gravity * simulationStep;

            // Misma lógica que ApplyForces: fórmula completa solo sobre y > 1m
            if (pos.y > 1f)
            {
                float speed = vel.magnitude;
                float dragMag = 0.5f * airDensity * speed * speed * dragCoeff * crossSectionArea;
                vel -= vel.normalized * (dragMag / mass) * simulationStep;
            }
            else
            {
                vel *= (1f - airDrag * simulationStep);
            }

            pos += vel * simulationStep;

            if (pos.y < 0f)
            {
                for (int j = i; j < trajectorySteps; j++)
                    points[j] = pos;
                break;
            }
        }

        lineRenderer.positionCount = trajectorySteps;
        lineRenderer.SetPositions(points);
    }

    Vector3 GetPredictedInitialVelocity()
    {
        Vector3 dir = Camera.main.transform.forward;
        dir.y = 0f;
        dir.Normalize();
        return dir * currentForce;
    }

    void ApplyImpulse(float force)
    {
        Vector3 dir = Camera.main.transform.forward;
        dir.y = 0f;
        dir.Normalize();
        velocity += dir * force;
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
        // Gravedad
        acceleration += Physics.gravity;

        // Fricción de rodadura
        if (velocity.magnitude > 0.01f)
        {
            Vector3 friction = -velocity.normalized * rollingFriction;
            acceleration += (friction / mass);
        }

        // Resistencia del aire:
        // - Por encima de y > 1m: fórmula física completa F = 0.5 * rho * v² * Cd * A
        // - Por debajo:           drag lineal simple (comportamiento original)
        if (transform.position.y > 1f)
        {
            float speed = velocity.magnitude;
            if (speed > 0.001f)
            {
                float dragMag = 0.5f * airDensity * speed * speed * dragCoeff * crossSectionArea;
                Vector3 dragForce = -velocity.normalized * dragMag;
                acceleration += dragForce / mass;
            }
        }
        else
        {
            Vector3 drag = -velocity * airDrag;
            acceleration += drag / mass;
        }
    }

    public void AddForce(Vector3 force)
    {
        acceleration += force / mass;
    }

    void ApplyVelocityDamping(float dt)
    {
        float speed = velocity.magnitude;
        if (speed > 0.001f)
        {
            float frictionFactor = 1f - (rollingFriction * dt);
            if (frictionFactor < 0f) frictionFactor = 0f;
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
                    transform.position += normal * penetration;

                    float vDot = Vector3.Dot(velocity, normal);
                    if (vDot < 0f)
                        velocity -= normal * vDot * 1.5f;
                }
            }
        }
    }
}

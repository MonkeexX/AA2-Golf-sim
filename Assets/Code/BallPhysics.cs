using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    [Header("Ball Properties")]
    public float mass = 1f;
    public float ballRadius = 0.5f;

    [Header("Air Resistence  F = ½?v²CdA")]
    public float airDensity = 1.225f;
    public float dragCoeff = 0.47f;

    [Header("Defaukt Friction (grass)")]
    public float defaultFriction = 0.4f;

    [Header("Velocity Visual Scale")]
    public float speedVisualScale = 25f;

    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public float currentFriction;
    [HideInInspector] public Vector3 angularVelocity;

    private Vector3 acceleration;
    private float crossSectionArea;
    private float inertia;             // I = (2/5)mr²
    private Vector3 groundNormal = Vector3.up;
    private bool isGrounded = false;

    private LevelLoader levelLoader;

    public void Init()
    {
        crossSectionArea = Mathf.PI * ballRadius * ballRadius;
        inertia = 0.4f * mass * ballRadius * ballRadius; // (2/5)mr²
        currentFriction = defaultFriction;
        levelLoader = FindObjectOfType<LevelLoader>();
    }

    public void Tick(float dt, Transform ballTransform)
    {
        currentFriction = defaultFriction;

        DetectGround(ballTransform);
        AccumulateForces(ballTransform);

        velocity += acceleration * dt;

        ApplyAirDamping(dt);
        UpdateAngularVelocity(dt, ballTransform);

        MoveWithCollisions(dt, ballTransform);
        HandleOverlapCollisions(ballTransform);

        acceleration = Vector3.zero;
    }

    void DetectGround(Transform t)
    {
        isGrounded = Physics.SphereCast(
            t.position,
            ballRadius * 0.9f,
            Physics.gravity.normalized,
            out RaycastHit hit,
            ballRadius * 1.2f
        );

        groundNormal = isGrounded ? hit.normal : Vector3.up;
    }

    void AccumulateForces(Transform t)
    {
        float g = Physics.gravity.magnitude;
        Vector3 gravityDir = Physics.gravity.normalized;

        if (isGrounded)
        {
            // Fparallel = mg·sin?  ?  move de ball thorugh the ramp
            // Fnormal   = mg·cos?  ?  perpendicular to floor
            Vector3 gravParallel = Vector3.ProjectOnPlane(gravityDir, groundNormal) * g;
            float cosTheta = Mathf.Max(0f, Vector3.Dot(-gravityDir, groundNormal));
            float normalForce = mass * g * cosTheta;

            acceleration += gravParallel;

            // ? = -µr · Fnormal · r  ?  a = µ·Fnormal / mass
            if (velocity.magnitude > 0.001f)
            {
                float frictionMag = currentFriction * normalForce / mass;
                acceleration -= velocity.normalized * frictionMag;
            }

            acceleration += -velocity * (0.02f / mass);
        }
        else
        {
            acceleration += Physics.gravity;

            // F = ½?v²CdA
            if (t.position.y > 1f)
            {
                float speed = velocity.magnitude;
                if (speed > 0.001f)
                {
                    float dragMag = 0.5f * airDensity * speed * speed
                                   * dragCoeff * crossSectionArea;
                    acceleration += -velocity.normalized * (dragMag / mass);
                }
            }
        }
    }

    void ApplyAirDamping(float dt)
    {
        if (isGrounded) return;
        float speed = velocity.magnitude;
        if (speed < 0.001f) { velocity = Vector3.zero; return; }
        velocity *= Mathf.Clamp01(1f - 0.01f * dt);
    }

    //  Angular velocity and visual rotation
    //  ? = v/r
    //  ? = -µr · Fnormal · r
    //  I = (2/5)mr²  ?  ? = ?/I
    void UpdateAngularVelocity(float dt, Transform t)
    {
        if (!isGrounded || velocity.magnitude < 0.001f)
        {
            angularVelocity *= 0.99f;
            t.Rotate(angularVelocity * Mathf.Rad2Deg * dt, Space.World);
            return;
        }

        float g = Physics.gravity.magnitude;
        float cosTheta = Mathf.Max(0f, Vector3.Dot(
                                  -Physics.gravity.normalized, groundNormal));
        float normalForce = mass * g * cosTheta;

        // Rolling Torque: ? = -µr · Fnormal · r
        float torqueMag = currentFriction * normalForce * ballRadius;

        // Angular Acceleration: ? = ? / I
        float alpha = torqueMag / inertia;

        Vector3 rotAxis = Vector3.Cross(
                                  velocity.normalized, groundNormal).normalized;

        float currentOmega = angularVelocity.magnitude;
        float targetOmega = velocity.magnitude / ballRadius;
        float newOmega = Mathf.MoveTowards(currentOmega, targetOmega, alpha * dt);

        angularVelocity = rotAxis * newOmega;

        t.Rotate(angularVelocity * Mathf.Rad2Deg * dt, Space.World);
    }

    void MoveWithCollisions(float dt, Transform t)
    {
        float remainingDist = velocity.magnitude * dt * speedVisualScale;
        Vector3 dir = velocity.normalized;
        int maxBounces = 3;

        while (remainingDist > 0.001f && maxBounces-- > 0)
        {
            if (Physics.SphereCast(t.position, ballRadius, dir,
                out RaycastHit hit, remainingDist))
            {
                t.position = hit.point + hit.normal * ballRadius;
                float e = GetRestitution(hit.collider);
                velocity = Vector3.Reflect(velocity, hit.normal) * e;
                dir = velocity.normalized;
                remainingDist -= hit.distance;

                if (hit.collider.CompareTag("Border"))
                    levelLoader?.RegisterBorderContact();
            }
            else
            {
                t.position += dir * remainingDist;
                break;
            }
        }
    }

    void HandleOverlapCollisions(Transform t)
    {
        foreach (Collider col in Physics.OverlapSphere(t.position, ballRadius))
        {
            if (col.attachedRigidbody != null &&
                !col.attachedRigidbody.isKinematic) continue;

            Vector3 closest = col.ClosestPoint(t.position);
            Vector3 dir = t.position - closest;
            float dist = dir.magnitude;
            if (dist < 0.0001f) continue;

            float penetration = ballRadius - dist;
            if (penetration <= 0f) continue;

            Vector3 normal = dir.normalized;
            t.position += normal * penetration;

            float vDot = Vector3.Dot(velocity, normal);
            if (vDot < 0f)
            {
                float e = GetRestitution(col);
                velocity -= normal * vDot * (1f + e);

                if (col.CompareTag("Border"))
                    levelLoader?.RegisterBorderContact();
            }
        }
    }

    public void AddForce(Vector3 force) => acceleration += force / mass;

    public void ApplyImpulse(Vector3 impulse) => velocity += impulse / mass;

    public void ResetState(Vector3 spawnPos, Transform t)
    {
        t.position = spawnPos;
        velocity = Vector3.zero;
        acceleration = Vector3.zero;
        angularVelocity = Vector3.zero;
        currentFriction = defaultFriction;
    }

    float GetRestitution(Collider col)
    {
        ObstacleProperties props = col.GetComponent<ObstacleProperties>();
        return props != null ? props.restitution : 0.6f;
    }
    public void DrawGizmos(Transform t)
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(t.position, groundNormal * 1.5f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(t.position, velocity.normalized);
    }
}
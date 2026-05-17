using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    [Header("Ball Properties")]
    public float mass = 1f;
    public float ballRadius = 0.5f;

    [Header("Shooting")]
    public float maxForcePower = 300f;
    public float chargeRate = 200f;

    [Header("Friction")]
    public float defaultFriction = 0.2f;   // fallback when no TerrainZone overrides

    [Header("Air Resistance  F = ½?v²CdA")]
    public float airDensity = 1.225f;
    public float dragCoeff = 0.47f;

    [Header("Trajectory Preview")]
    public LineRenderer lineRenderer;
    public int trajectorySteps = 40;
    public float simulationStep = 0.05f;

    [Header("Visual Scale")]
    public float speedVisualScale = 10f;

    [Header("Slope / Ramp")]
    public float groundCheckDistance = 0.6f; // un poco más que el radio
    private Vector3 groundNormal = Vector3.up;
    private bool isGrounded = false;

    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public float currentFriction;   // set each frame by TerrainZone

    private Vector3 acceleration;
    private float crossSectionArea;
    private float currentForce;
    private bool isCharging;
    private bool canShoot = true;

    public System.Action OnShotFired;

    void Awake()
    {
        crossSectionArea = Mathf.PI * ballRadius * ballRadius;
        currentFriction = defaultFriction;
    }

    void Update()
    {
        if (!canShoot) return;

        HandleInput();

        currentFriction = defaultFriction;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        AccumulateForces();

        velocity += acceleration * dt;

        ApplyFrictionDamping(dt);

        MoveWithCollisions(dt);

        HandleOverlapCollisions();

        acceleration = Vector3.zero;
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currentForce = 0f;
        }

        if (Input.GetMouseButton(0) && isCharging)
            currentForce = Mathf.Clamp(currentForce + chargeRate * Time.deltaTime, 0f, maxForcePower);

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            FireImpulse(currentForce);
            isCharging = false;
            currentForce = 0f;
        }

        if (isCharging)
            DrawTrajectory();
        else if (lineRenderer != null)
            lineRenderer.positionCount = 0;
    }

    void FireImpulse(float force)
    {
        velocity += AimDirection() * force;
        OnShotFired?.Invoke();
    }

    Vector3 AimDirection()
    {
        Vector3 dir = Camera.main.transform.forward;
        dir.y = 0f;
        return dir.normalized;
    }
    void AccumulateForces()
    {
        DetectGround();

        if (isGrounded)
        {
            // F_parallel = mg·sin?  ?  componente que acelera la bola cuesta abajo
            // F_normal   = mg·cos?  ?  componente perpendicular (no mueve la bola)
            Vector3 gravityDir = Physics.gravity.normalized;
            float g = Physics.gravity.magnitude;

            Vector3 gravParallel = Vector3.ProjectOnPlane(gravityDir, groundNormal) * g;
            acceleration += gravParallel;

            // Fricción en rampa usa F_normal = mg·cos?
            float cosTheta = Vector3.Dot(-gravityDir, groundNormal); // = cos?
            float normalForce = mass * g * cosTheta;
            float frictionDecel = currentFriction * normalForce / mass;   // µ·g·cos?

            if (velocity.magnitude > 0.001f)
                acceleration -= velocity.normalized * frictionDecel;

            acceleration += -velocity * (0.05f / mass);
        }
        else
        {
            acceleration += Physics.gravity;

            float speed = velocity.magnitude;
            if (speed > 0.001f)
            {
                float dragMag = 0.5f * airDensity * speed * speed * dragCoeff * crossSectionArea;
                acceleration += -velocity.normalized * (dragMag / mass);
            }
        }
    }

    void DetectGround()
    {
        if (Physics.SphereCast(transform.position, ballRadius * 0.9f, Physics.gravity.normalized,
            out RaycastHit hit, groundCheckDistance))
        {
            groundNormal = hit.normal;
            isGrounded = true;
        }
        else
        {
            groundNormal = Vector3.up;
            isGrounded = false;
        }
    }

    void ApplyFrictionDamping(float dt)
    {
        if (isGrounded) return; 

        float speed = velocity.magnitude;
        if (speed < 0.001f) { velocity = Vector3.zero; return; }
        velocity *= Mathf.Clamp01(1f - 0.01f * dt);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, groundNormal * 1.5f);
    }

    void MoveWithCollisions(float dt)
    {
        float remainingDist = velocity.magnitude * dt * speedVisualScale; 
        Vector3 dir = velocity.normalized;
        int maxBounces = 3;

        while (remainingDist > 0.001f && maxBounces-- > 0)
        {
            if (Physics.SphereCast(transform.position, ballRadius, dir, out RaycastHit hit, remainingDist))
            {
                transform.position = hit.point + hit.normal * ballRadius;
                float e = GetRestitution(hit.collider);
                velocity = Vector3.Reflect(velocity, hit.normal) * e;
                dir = velocity.normalized;
                remainingDist -= hit.distance;
            }
            else
            {
                transform.position += dir * remainingDist;
                break;
            }
        }
    }

    void HandleOverlapCollisions()
    {
        foreach (Collider col in Physics.OverlapSphere(transform.position, ballRadius))
        {
            if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic) continue;

            Vector3 closest = col.ClosestPoint(transform.position);
            Vector3 dir = transform.position - closest;
            float dist = dir.magnitude;
            if (dist < 0.0001f) continue;

            float penetration = ballRadius - dist;
            if (penetration <= 0f) continue;

            Vector3 normal = dir.normalized;
            transform.position += normal * penetration;

            float vDot = Vector3.Dot(velocity, normal);
            if (vDot < 0f)
            {
                float e = GetRestitution(col);
                velocity -= normal * vDot * (1f + e);
            }
        }
    }

    float GetRestitution(Collider col)
    {
        ObstacleProperties props = col.GetComponent<ObstacleProperties>();
        return props != null ? props.restitution : 0.6f;
    }


    public void AddForce(Vector3 force) => acceleration += force / mass;

    public void SetCanShoot(bool value)
    {
        canShoot = value;
        if (!value && lineRenderer != null)
            lineRenderer.positionCount = 0;
    }

    public void ResetToPosition(Vector3 pos)
    {
        transform.position = pos;
        velocity = Vector3.zero;
        acceleration = Vector3.zero;
        currentFriction = defaultFriction;
        currentForce = 0f;
        isCharging = false;
        if (lineRenderer != null) lineRenderer.positionCount = 0;
    }

    void DrawTrajectory()
    {
        if (lineRenderer == null) return;

        Vector3 pos = transform.position;
        Vector3 vel = AimDirection() * currentForce;
        Vector3[] points = new Vector3[trajectorySteps];

        for (int i = 0; i < trajectorySteps; i++)
        {
            points[i] = pos;

            vel += Physics.gravity * simulationStep;

            if (pos.y > 1f)
            {
                float speed = vel.magnitude;
                float dragMag = 0.5f * airDensity * speed * speed * dragCoeff * crossSectionArea;
                vel -= vel.normalized * (dragMag / mass) * simulationStep;
            }

            pos += vel * simulationStep;

            if (pos.y < 0f)
            {
                for (int j = i; j < trajectorySteps; j++) points[j] = pos;
                break;
            }
        }

        lineRenderer.positionCount = trajectorySteps;
        lineRenderer.SetPositions(points);
    }
}
using UnityEngine;

/// <summary>
/// Custom physics engine for the golf ball.
/// Implements: gravity, rolling friction, air drag (y > 1m), impulse, collision response.
/// No Unity Rigidbody used.
/// </summary>
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

    // ?? Runtime state ????????????????????????????????????????????????
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public float currentFriction;   // set each frame by TerrainZone

    private Vector3 acceleration;
    private float crossSectionArea;
    private float currentForce;
    private bool isCharging;
    private bool canShoot = true;

    // ?? Events ???????????????????????????????????????????????????????
    public System.Action OnShotFired;

    // ?????????????????????????????????????????????????????????????????
    void Awake()
    {
        crossSectionArea = Mathf.PI * ballRadius * ballRadius;
        currentFriction = defaultFriction;
    }

    void Update()
    {
        if (!canShoot) return;

        HandleInput();

        // Reset friction each frame; TerrainZone.Update() will override if ball is inside
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

    // ?????????????????????????????????????????????????????????????????
    //  Input & shooting
    // ?????????????????????????????????????????????????????????????????
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

    // ?????????????????????????????????????????????????????????????????
    //  Physics integration
    // ?????????????????????????????????????????????????????????????????
    void AccumulateForces()
    {
        // Gravity
        acceleration += Physics.gravity;

        // Air drag — full quadratic above 1 m, simple linear on ground
        if (transform.position.y > 1f)
        {
            float speed = velocity.magnitude;
            if (speed > 0.001f)
            {
                float dragMag = 0.5f * airDensity * speed * speed * dragCoeff * crossSectionArea;
                acceleration += -velocity.normalized * (dragMag / mass);
            }
        }
        else
        {
            // Light linear drag while rolling (air term only; friction handled separately)
            acceleration += -velocity * (0.05f / mass);
        }
    }

    /// <summary>
    /// Applies rolling-friction deceleration directly to velocity magnitude.
    /// Called once per FixedUpdate, AFTER gravity/drag integration.
    /// </summary>
    void ApplyFrictionDamping(float dt)
    {
        float speed = velocity.magnitude;
        if (speed < 0.001f) { velocity = Vector3.zero; return; }

        float frictionDecel = currentFriction * Mathf.Abs(Physics.gravity.y); // µ * g
        float newSpeed = Mathf.Max(0f, speed - frictionDecel * dt);
        velocity = velocity.normalized * newSpeed;
    }

    // ?????????????????????????????????????????????????????????????????
    //  Movement & collisions
    // ?????????????????????????????????????????????????????????????????
    void MoveWithCollisions(float dt)
    {
        float remainingDist = velocity.magnitude * dt;
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

    // ?????????????????????????????????????????????????????????????????
    //  Public API
    // ?????????????????????????????????????????????????????????????????
    /// <summary>External forces (wind zones, ramps, etc.)</summary>
    public void AddForce(Vector3 force) => acceleration += force / mass;

    public void SetCanShoot(bool value)
    {
        canShoot = value;
        if (!value && lineRenderer != null)
            lineRenderer.positionCount = 0;
    }

    /// <summary>Teleport ball and reset motion — used by LevelLoader.</summary>
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

    // ?????????????????????????????????????????????????????????????????
    //  Trajectory preview
    // ?????????????????????????????????????????????????????????????????
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
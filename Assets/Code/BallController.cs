using UnityEngine;

/// <summary>
/// Gestiona el input del jugador y la trayectoria predictiva.
/// Mecánica de click y arrastrar: el jugador arrastra el ratón para
/// apuntar y cargar fuerza. Delega toda la física a PhysicsManager.
/// La flecha de dirección se genera por código, sin prefab.
/// </summary>
[RequireComponent(typeof(PhysicsManager))]
public class BallController : MonoBehaviour
{
    [Header("Disparo")]
    public float maxForcePower = 8f;
    public float chargeRate = 6f;
    public float maxDragDistance = 3f;

    [Header("Trayectoria Predictiva")]
    public LineRenderer lineRenderer;
    public int trajectorySteps = 40;
    public float simulationStep = 0.05f;

    // ?? Referencias ???????????????????????????????????????????????????
    [HideInInspector] public PhysicsManager physics;

    private float currentForce;
    private bool isCharging;
    private bool canShoot = true;
    private Vector3 dragStartWorld;
    private Vector3 currentAimDirection = Vector3.forward;
    private LineRenderer arrowLine;

    // Evento que notifica al LevelLoader cuando se dispara
    public System.Action OnShotFired;

    // ?????????????????????????????????????????????????????????????????
    void Awake()
    {
        physics = GetComponent<PhysicsManager>();
        physics.Init();
        InitArrow();
    }

    void Update()
    {
        if (!canShoot) return;
        HandleInput();
        ShowArrow();
    }

    void FixedUpdate()
    {
        physics.Tick(Time.fixedDeltaTime, transform);
    }

    // ?????????????????????????????????????????????????????????????????
    //  Flecha generada por código
    // ?????????????????????????????????????????????????????????????????
    void InitArrow()
    {
        GameObject arrowObj = new GameObject("Arrow");
        arrowLine = arrowObj.AddComponent<LineRenderer>();
        arrowLine.positionCount = 2;
        arrowLine.startWidth = 0.1f;
        arrowLine.endWidth = 0.25f;
        arrowLine.useWorldSpace = true;
        arrowLine.material = new Material(Shader.Find("Sprites/Default"));
        arrowLine.startColor = Color.green;
        arrowLine.endColor = Color.red;
        arrowLine.gameObject.SetActive(false);
    }

    void ShowArrow()
    {
        if (arrowLine == null) return;

        bool show = isCharging && currentForce > 0.1f;
        arrowLine.gameObject.SetActive(show);
        if (!show) return;

        // La flecha crece con la fuerza cargada
        float normalizedForce = currentForce / maxForcePower;
        float arrowLength = 0.5f + normalizedForce * 2f;
        Vector3 start = transform.position;
        Vector3 end = transform.position
                                + currentAimDirection * arrowLength;

        arrowLine.SetPosition(0, start);
        arrowLine.SetPosition(1, end);
    }

    // ?????????????????????????????????????????????????????????????????
    //  Input del jugador — click y arrastrar
    // ?????????????????????????????????????????????????????????????????
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currentForce = 0f;
            dragStartWorld = GetMouseWorldPos();
            currentAimDirection = Vector3.forward;
        }

        if (Input.GetMouseButton(0) && isCharging)
        {
            Vector3 dragEnd = GetMouseWorldPos();
            Vector3 drag = dragStartWorld - dragEnd;
            drag.y = 0f;

            if (drag.magnitude > 0.1f)
            {
                currentAimDirection = drag.normalized;

                // Normalizar el arrastre entre 0 y maxDragDistance
                // así la fuerza es proporcional y predecible
                float normalizedDrag = Mathf.Clamp01(drag.magnitude / maxDragDistance);
                currentForce = normalizedDrag * maxForcePower;
            }
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            Vector3 impulse = currentAimDirection * currentForce * physics.mass;
            physics.ApplyImpulse(impulse);

            OnShotFired?.Invoke();

            isCharging = false;
            currentForce = 0f;

            if (arrowLine != null)
                arrowLine.gameObject.SetActive(false);

            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }

        if (isCharging)
            DrawTrajectory();
        else if (lineRenderer != null)
            lineRenderer.positionCount = 0;
    }

    // Proyecta el ratón sobre el plano horizontal de la bola
    Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        plane.Raycast(ray, out float dist);
        return ray.GetPoint(dist);
    }

    // ?????????????????????????????????????????????????????????????????
    //  Trayectoria predictiva
    // ?????????????????????????????????????????????????????????????????
    void DrawTrajectory()
    {
        if (lineRenderer == null || currentForce < 0.1f) return;

        Vector3 pos = transform.position;
        Vector3 vel = currentAimDirection * currentForce;
        Vector3[] points = new Vector3[trajectorySteps];
        float area = Mathf.PI * physics.ballRadius * physics.ballRadius;

        for (int i = 0; i < trajectorySteps; i++)
        {
            points[i] = pos;
            vel += Physics.gravity * simulationStep;

            if (pos.y > 1f)
            {
                float dragMag = 0.5f * physics.airDensity * vel.sqrMagnitude
                               * physics.dragCoeff * area;
                vel -= vel.normalized * (dragMag / physics.mass) * simulationStep;
            }

            pos += vel * simulationStep * physics.speedVisualScale;

            if (pos.y < 0f)
            {
                for (int j = i; j < trajectorySteps; j++) points[j] = pos;
                break;
            }
        }

        lineRenderer.positionCount = trajectorySteps;
        lineRenderer.SetPositions(points);
    }

    // ?????????????????????????????????????????????????????????????????
    //  API pública
    // ?????????????????????????????????????????????????????????????????
    public void SetCanShoot(bool value)
    {
        canShoot = value;
        if (!value)
        {
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
            if (arrowLine != null)
                arrowLine.gameObject.SetActive(false);
        }
    }

    public void ResetToPosition(Vector3 pos)
    {
        physics.ResetState(pos, transform);
        currentForce = 0f;
        isCharging = false;
        currentAimDirection = Vector3.forward;
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
        if (arrowLine != null)
            arrowLine.gameObject.SetActive(false);
    }

    public Vector3 GetVelocity() => physics.velocity;

    void OnDrawGizmos()
    {
        if (physics != null) physics.DrawGizmos(transform);
    }
}
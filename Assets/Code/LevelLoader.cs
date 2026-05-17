using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detecta entrada al hoyo por distancia (sin Trigger ni Rigidbody).
/// Condiciones de victoria:
///   - Velocidad al entrar menor a 0.5 m/s
///   - Máximo 2 contactos con el borde en el último disparo
/// </summary>
public class LevelLoader : MonoBehaviour
{
    [Header("Referencias")]
    public BallController ball;
    public Transform ballSpawnPoint;

    [Header("Condición de Victoria")]
    public float maxEntrySpeed = 0.5f;
    public float holeRadius = 0.8f;
    public int maxBorderContacts = 2;

    [Header("Navegación de Escenas")]
    public string nextSceneName;
    public GameObject winUI;
    public GameObject failUI;

    private bool levelFinished = false;
    private int borderContactCount = 0;

    // ?????????????????????????????????????????????????????????????????
    void Start()
    {
        if (winUI) winUI.SetActive(false);
        if (failUI) failUI.SetActive(false);

        if (ball != null && ballSpawnPoint != null)
            ball.ResetToPosition(ballSpawnPoint.position);

        Debug.Log($"[LevelLoader] Nivel iniciado. " +
                  $"Hoyo en {transform.position}, radio={holeRadius}");
    }

    // ?????????????????????????????????????????????????????????????????
    void Update()
    {
        if (levelFinished || ball == null) return;

        float dist = Vector3.Distance(
                          ball.transform.position, transform.position);
        float speed = ball.GetVelocity().magnitude;

        Debug.Log($"[LevelLoader] Dist hoyo: {dist:F2} | " +
                  $"Vel: {speed:F2} m/s | " +
                  $"Rebotes borde: {borderContactCount}");

        if (dist > holeRadius) return;

        Debug.Log($"[LevelLoader] Bola en el hoyo. " +
                  $"Speed={speed:F2} | Rebotes={borderContactCount}");

        levelFinished = true;

        bool speedOk = speed <= maxEntrySpeed;
        bool rebotesOk = borderContactCount <= maxBorderContacts;

        if (speedOk && rebotesOk)
        {
            Debug.Log("[LevelLoader] ? VICTORIA");
            HandleWin();
        }
        else
        {
            if (!speedOk)
                Debug.Log($"[LevelLoader] ? Vel {speed:F2} > {maxEntrySpeed}");
            if (!rebotesOk)
                Debug.Log($"[LevelLoader] ? Rebotes {borderContactCount} > {maxBorderContacts}");
            HandleFail();
        }
    }

    // ?????????????????????????????????????????????????????????????????
    public void RegisterBorderContact()
    {
        borderContactCount++;
        Debug.Log($"[LevelLoader] Contacto con borde #{borderContactCount}");
    }

    // ?????????????????????????????????????????????????????????????????
    void HandleWin()
    {
        ball.SetCanShoot(false);

        if (!string.IsNullOrEmpty(nextSceneName))
            Invoke(nameof(LoadNextLevel), 1.5f);
        else
            if (winUI) winUI.SetActive(true);
    }

    void HandleFail()
    {
        levelFinished = false;
        if (failUI) failUI.SetActive(true);
        Invoke(nameof(HideFailUI), 1.5f);
    }

    void LoadNextLevel() => SceneManager.LoadScene(nextSceneName);

    void HideFailUI()
    {
        if (failUI) failUI.SetActive(false);
    }

    public void RestartLevel() =>
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, holeRadius);
    }
}
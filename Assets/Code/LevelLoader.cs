using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    [Header("References")]
    public BallPhysics ball;
    public Transform ballSpawnPoint;

    [Header("Win Condition")]
    public float maxEntrySpeed = 0.5f;
    public float holeRadius = 0.8f;

    [Header("Scene Navigation")]
    public string nextSceneName;
    public GameObject winUI;
    public GameObject failUI;

    private bool levelFinished = false;

    void Start()
    {
        if (winUI) winUI.SetActive(false);
        if (failUI) failUI.SetActive(false);

        if (ball != null && ballSpawnPoint != null)
            ball.ResetToPosition(ballSpawnPoint.position);

        Debug.Log($"[LevelLoader] Iniciado. Hole en {transform.position}, radio={holeRadius}");
    }

    void Update()
    {
        if (levelFinished || ball == null) return;

        float dist = Vector3.Distance(ball.transform.position, transform.position);

        // Log continuo para ver distancia en tiempo real
        Debug.Log($"[LevelLoader] Distancia al hoyo: {dist:F2} | Velocidad: {ball.velocity.magnitude:F2} m/s");

        if (dist <= holeRadius)
        {
            float speed = ball.velocity.magnitude;
            Debug.Log($"[LevelLoader] ¡Bola dentro del radio! Speed={speed:F2} | Límite={maxEntrySpeed}");

            levelFinished = true;

            if (speed <= maxEntrySpeed)
            {
                Debug.Log("[LevelLoader] ? CONDICIÓN CUMPLIDA ? Pasando de nivel");
                HandleWin();
            }
            else
            {
                Debug.Log($"[LevelLoader] ? Demasiado rápida ({speed:F2} > {maxEntrySpeed}) ? Fail");
                HandleFail();
            }
        }
    }

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

    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    // Dibuja el radio del hoyo en la Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, holeRadius);
    }
}
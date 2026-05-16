using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detects when the ball enters the hole trigger.
/// Win condition: speed < 0.5 m/s on entry.
/// Loads the next scene or shows a "you win" screen on the last level.
/// Attach this to the Hole GameObject (with a trigger collider).
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelLoader : MonoBehaviour
{
    [Header("References")]
    public BallPhysics ball;
    public Transform ballSpawnPoint;

    [Header("Win Condition")]
    public float maxEntrySpeed = 0.5f;

    [Header("Scene Navigation")]
    public string nextSceneName;          // leave empty on last level
    public GameObject winUI;
    public GameObject failUI;

    // ?? Level-start ???????????????????????????????????????????????????
    void Start()
    {
        if (winUI) winUI.SetActive(false);
        if (failUI) failUI.SetActive(false);

        if (ball != null && ballSpawnPoint != null)
            ball.ResetToPosition(ballSpawnPoint.position);
    }

    // ?? Hole detection ????????????????????????????????????????????????
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;   // tag your ball GameObject "Ball"

        float speed = ball.velocity.magnitude;

        if (speed <= maxEntrySpeed)
            HandleWin();
        else
            HandleFail();
    }

    void HandleWin()
    {
        ball.SetCanShoot(false);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // Small delay so the player sees the ball drop in
            Invoke(nameof(LoadNextLevel), 1.5f);
        }
        else
        {
            if (winUI) winUI.SetActive(true);   // final level
        }
    }

    void HandleFail()
    {
        // Too fast — bounce out is handled by physics; just show feedback
        if (failUI) failUI.SetActive(true);
        Invoke(nameof(HideFailUI), 1.5f);
    }

    void LoadNextLevel() => SceneManager.LoadScene(nextSceneName);

    void HideFailUI()
    {
        if (failUI) failUI.SetActive(false);
    }

    // ?? Retry / restart helpers (call from UI buttons) ????????????????
    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void LoadScene(string name) => SceneManager.LoadScene(name);
}
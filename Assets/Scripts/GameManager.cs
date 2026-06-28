using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject player;
    private PlayerController playerController;

    [Header("UI Reference")]
    [Tooltip("The GameObject of the Game Over canvas")]
    public GameObject gameOverUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ensure failure UI is hidden at start
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
        playerController = player.GetComponent<PlayerController>();
    }

    public void GameOver()
    {
        Debug.Log("Game Over triggered in GameManager!");
        
        // Show the failure UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
        else
        {
            Debug.LogError("Game Over UI is not assigned in the GameManager!");
        }
    }

    public void RestartGame()
    {
        Debug.Log("Restarting scene...");
        // Reload current scene
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        gameOverUI.SetActive(false);
        player.transform.position = PlayerController.startPosition;
        foreach (var trail in playerController.GetSpawnedTrails())
        {
            Destroy(trail);
        }
        playerController.SetIsDead(false);
        playerController.SetCurrentDirection(Vector3.forward);
    }
}

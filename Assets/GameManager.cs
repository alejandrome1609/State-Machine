using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject resultPanel;
    public TMP_Text resultText;
    public PlayerController playerController;
    public BossController bossController;
    public GameObject playerHealthBar;
    public GameObject bossHealthBar;

    private bool gameEnded = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        resultPanel.SetActive(false);
    }

    public void EndGame(bool playerWon)
    {
        if (gameEnded == true)
        {
            return;
        }

        gameEnded = true;

        if (playerWon == true)
        {
            resultText.text = "Nice :D";
        }
        else
        {
            resultText.text = "Womp";
        }

        playerController.enabled = false;
        bossController.enabled = false;

        //apparently disabling the boss script alone doesn't stop its NavMeshAgent, added it cuz why not.
        bossController.agent.isStopped = true;
        playerHealthBar.SetActive(false);
        bossHealthBar.SetActive(false);
        resultPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Update is called once per frame
    void Update()
    {

    }
}

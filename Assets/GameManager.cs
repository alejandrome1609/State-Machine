using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject resultPanel;
    public TMP_Text resultText;

    public PlayerController playerController;
    public BossController bossController;

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
            resultText.text = "You Win!";
        }
        else
        {
            resultText.text = "You Lose!";
        }

        playerController.enabled = false;
        bossController.enabled = false;

        //apparently disabling the boss script alone doesn't stop its NavMeshAgent.
        bossController.agent.isStopped = true;

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

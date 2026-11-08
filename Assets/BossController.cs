using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    private enum BossState
    {
        Chase,
        Windup,
        Attack,
        Recover,
        Stunned,
        Dead
    }
    public float windupDuration = 1f;
    public float attackDuration = 0.2f;
    public float recoveryDuration = 1.5f;
    private float stateTimeRemaining = 0f;
    public PlayerController playerController;
    public int attackDamage = 10;
    public float attackRange = 6.5f;
    public float stunDuration = 2f;
    public int health = 300;
    public GameManager gameManager;
    public Slider healthBar;
    public void TakeDamage(int damage)
    {
        if (currentState == BossState.Dead)
        {
            return;
        }

        health = health - damage;

        if (health <= 0)
        {
            health = 0;
            SetState(BossState.Dead);
        }
        healthBar.value = health;
    }


    public Renderer bossRenderer;
    private BossState currentState = BossState.Chase;
    private void SetState(BossState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        if (currentState == BossState.Chase)
        {
            agent.isStopped = false;
            bossRenderer.material.color = Color.white;
        }
        else if (currentState == BossState.Windup)
        {
            agent.isStopped = true;
            stateTimeRemaining = windupDuration;
            bossRenderer.material.color = Color.yellow;
        }
        else if (currentState == BossState.Attack)
        {
            agent.isStopped = true;
            stateTimeRemaining = attackDuration;
            bossRenderer.material.color = Color.red;

            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                playerController.TakeDamage(attackDamage);
            }
        }
        else if (currentState == BossState.Recover)
        {
            agent.isStopped = true;
            stateTimeRemaining = recoveryDuration;
            bossRenderer.material.color = Color.blue;
        }
        else if (currentState == BossState.Stunned)
        {
            agent.isStopped = true;
            stateTimeRemaining = stunDuration;
            bossRenderer.material.color = Color.magenta;
        }
        else if (currentState == BossState.Dead)
        {
            agent.isStopped = true;
            bossRenderer.material.color = Color.gray;
            gameManager.EndGame(true);
        }
    }
    public void GetParried()
    {
        if (currentState == BossState.Dead)
        {
            return;
        }

        SetState(BossState.Stunned);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthBar.maxValue = health;
        healthBar.value = health;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == BossState.Dead)
        {
            return;
        }

        BossState nextState = currentState;

        if (currentState == BossState.Chase)
        {
            agent.SetDestination(player.position);

            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= agent.stoppingDistance + 0.5f)
            {
                nextState = BossState.Windup;
            }
        }
        else
        {
            stateTimeRemaining = stateTimeRemaining - Time.deltaTime;

            if (stateTimeRemaining <= 0f)
            {
                if (currentState == BossState.Windup)
                {
                    nextState = BossState.Attack;
                }
                else if (currentState == BossState.Attack)
                {
                    nextState = BossState.Recover;
                }
                else if (currentState == BossState.Recover)
                {
                    nextState = BossState.Chase;
                }
                else if (currentState == BossState.Stunned)
                {
                    nextState = BossState.Chase;
                }
            }
        }

        SetState(nextState);
    }
}

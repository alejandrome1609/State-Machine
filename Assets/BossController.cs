using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    //recieved feedback that the boss should actually follow the player, so, these references here are the navmesh agent that moves our boss and the player it follows.
    public NavMeshAgent agent;
    public Transform player;
    //same idea as the player, these are the different states the boss can be in.
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
    public Animator weaponAnimator;
    public float sideSwingWindupDuration = 1f;
    //the player calls this when landing an attack, passing in how much damage it does.
    public void TakeDamage(int damage)
    {
        if (currentState == BossState.Dead)
        {
            return;
        }

        health = health - damage;

        //keeps health from showing negative numbers and enters the dead state.
        if (health <= 0)
        {
            health = 0;
            SetState(BossState.Dead);
        }
        healthBar.value = health;
    }
    //boss starts by chasing the player.
    private BossState currentState = BossState.Chase;
    //just like the player script, state changes go through this function.
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
        }
        else if (currentState == BossState.Windup)
        {
            //boss stops walking while preparing an attack. also restores animation speed in case a parry froze it earlier.
            agent.isStopped = true;
            weaponAnimator.speed = 1f;
            //this picks either 0 or 1, since the upper number isnt included for integers. we only pick when entering windup
            int attackChoice = Random.Range(0, 2);

            if (attackChoice == 0)
            {
                //overhead takes longer to reach the hit, so it gets its own timer.
                stateTimeRemaining = windupDuration;
                weaponAnimator.Play("BossOverhead", 0, 0f);
            }
            else
            {
                //same thing, but with the side swing and its shorter windup.
                stateTimeRemaining = sideSwingWindupDuration;
                weaponAnimator.Play("BossSideSwing", 0, 0f);
            }
        }
        else if (currentState == BossState.Attack)
        {
            agent.isStopped = true;
            stateTimeRemaining = attackDuration;
            //checks if the player is close enough at the moment the attack lands.
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
        }
        else if (currentState == BossState.Stunned)
        {
            //a successful parry stops both movement and the weapon animation.
            agent.isStopped = true;
            stateTimeRemaining = stunDuration;
            weaponAnimator.speed = 0f;
        }
        else if (currentState == BossState.Dead)
        {
            agent.isStopped = true;
            gameManager.EndGame(true);
            weaponAnimator.speed = 0f;
        }
    }
    //the player calls this after a successful parry.
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
        //dead bosses shouldnt be chasing or attacking anyone lol.
        if (currentState == BossState.Dead)
        {
            return;
        }
        //start by assuming we stay in our current state, then check if we need to change.
        BossState nextState = currentState;

        if (currentState == BossState.Chase)
        {
            //updates the destination so the boss follows the player as they move.
            agent.SetDestination(player.position);

            float distance = Vector3.Distance(transform.position, player.position);
            //the extra 0.5 gives a little room around the stopping distance so the boss can begin attacking without needing to get perfectly close.
            if (distance <= agent.stoppingDistance + 0.5f)
            {
                nextState = BossState.Windup;
            }
        }
        else
        {
            //the other living states use a countdown to decide when they finish
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
        //applies whatever state we decided on after checking everything above.
        SetState(nextState);
    }
}

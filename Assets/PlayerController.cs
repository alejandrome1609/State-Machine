using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public Animator animator;
    public CharacterController controller;
    public Transform boss;

    public float moveSpeed = 3f;
    public float attackDuration = 1f;
    private float attackTimeRemaining = 0f;
    public float raiseDuration = 1.23f;
    private float raiseTimeRemaining = 0f;
    private float chargeTime = 0f;
    public float heavyAttackDuration = 3.6f;
    private float heavyAttackTimeRemaining = 0f;
    private bool chargeReleased = false;
    public int health = 100;
    public float parryDuration = 0.25f;
    private float parryTimeRemaining = 0f;
    public BossController bossController;
    public float attackHitDelay = 0.7f;
    private float attackHitTimeRemaining = 0f;
    private bool attackHitChecked = false;

    public int attackDamage = 10;
    public float attackRange = 6.5f;
    public int heavyBaseDamage = 20;
    public float maxChargeTime = 2f;
    public float heavyHitDelay = 0.3f;

    private float heavyHitTimeRemaining = 0f;
    private bool heavyHitChecked = false;
    private int chargedDamage = 0;
    public GameManager gameManager;
    public float blockCooldown = 0.4f;
    private float blockCooldownRemaining = 0f;
    public ParticleSystem fullChargeEffect;
    private bool fullChargeShown = false;
    public Slider healthBar;
    public Image parryFlash;
    private float parryFlashAlpha = 0f;
    public float blockProtectionDelay = 0.5f;
    private float blockTimeHeld = 0f;

    private enum PlayerState
    {
        Idle,
        Move,
        Block,
        Attack,
        Charge,
        HeavyAttack,
        Dead
    }

    private PlayerState currentState = PlayerState.Idle;
    public void TakeDamage(int damage)
    {
        if (currentState == PlayerState.Dead)
        {
            return;
        }

        if (currentState == PlayerState.Block)
        {
            if (parryTimeRemaining > 0f)
            {
                parryTimeRemaining = 0f;

                parryFlashAlpha = 0.3f;
                parryFlash.color = new Color(1f, 1f, 1f, parryFlashAlpha);

                bossController.GetParried();
                return;
            }

            if (blockTimeHeld >= blockProtectionDelay)
            {
                damage = 10;
            }
        }

        health = health - damage;

        if (health <= 0)
        {
            health = 0;
            SetState(PlayerState.Dead);
        }
        healthBar.value = health;
    }

    //all state changes go through this function, makes code clean.
    private void SetState(PlayerState newState)
    {
        //in english this pretty much says: "if we're already in this state, don't restart its animation or timer." it will then allow animations to flow smoothly and not get interrupted.
        if (currentState == newState)
        {
            return;
        }
        //this allows eaving block to start a cooldown.
        if (currentState == PlayerState.Block)
        {
            blockCooldownRemaining = blockCooldown;
        }
        currentState = newState;
        animator.speed = 1f;

        if (currentState == PlayerState.Idle)
        {
            animator.Play("Idle");
        }
        else if (currentState == PlayerState.Move)
        {
            animator.Play("Move");
        }
        else if (currentState == PlayerState.Block)
        {
            blockTimeHeld = 0f;
            parryTimeRemaining = parryDuration;
            animator.Play("Block");
        }
        else if (currentState == PlayerState.Attack)
        {
            attackTimeRemaining = attackDuration / 0.8f;
            attackHitTimeRemaining = attackHitDelay / 0.8f;
            attackHitChecked = false;

            animator.speed = 0.8f;
            animator.Play("Attack", 0, 0f); 
        }
        else if (currentState == PlayerState.Charge)
        {
            raiseTimeRemaining = raiseDuration / 2f;
            chargeTime = 0f;
            fullChargeShown = false;
            fullChargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            chargeReleased = false;

            animator.speed = 2f;
            animator.Play("HeavyAttack", 0, 0f);
        }
        else if (currentState == PlayerState.HeavyAttack)
        {
            heavyAttackTimeRemaining = heavyAttackDuration / 1.5f;
            heavyHitTimeRemaining = heavyHitDelay / 1.5f;
            heavyHitChecked = false;
            fullChargeEffect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);

            
            float countedCharge = chargeTime;

            if (countedCharge > maxChargeTime)
            {
                countedCharge = maxChargeTime;
            }

            chargedDamage = heavyBaseDamage + Mathf.RoundToInt((countedCharge/ maxChargeTime) * 30f);

           
            animator.speed = 1.5f;
        }
        else if (currentState == PlayerState.Dead)
        {
            animator.Play("Idle");
            gameManager.EndGame(false);
        }
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
        if (parryFlashAlpha > 0f)
        {
            parryFlashAlpha = parryFlashAlpha - Time.deltaTime * 3f;

            if (parryFlashAlpha < 0f)
            {
                parryFlashAlpha = 0f;
            }

            parryFlash.color = new Color(1f, 1f, 1f, parryFlashAlpha);
        }
        if (currentState == PlayerState.Dead)
        {
            return;
        }
        if (currentState == PlayerState.Block)
        {
            blockTimeHeld = blockTimeHeld + Time.deltaTime;
        }
        if (blockCooldownRemaining > 0f)
        {
            blockCooldownRemaining = blockCooldownRemaining - Time.deltaTime;
        }
        //this allows the player to constantly face the boss while keeping y at zero so theres no tilting and funny buisiness.
        Vector3 bossDirection = boss.position - transform.position;
        bossDirection.y = 0f;

        if (bossDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(bossDirection);
        }
        if (parryTimeRemaining > 0f)
        {
            parryTimeRemaining = parryTimeRemaining - Time.deltaTime;
        }
        //this reads each movement fresh each frame.
        float sideways = 0f;
        float forwards = 0f;

        if (Keyboard.current.aKey.isPressed)
        {
            sideways = sideways - 1f;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            sideways = sideways + 1f;
        }

        if (Keyboard.current.wKey.isPressed)
        {
            forwards = forwards + 1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            forwards = forwards - 1f;
        }

        Vector3 movement = transform.right * sideways + transform.forward * forwards;

        //here I actually decided to limit  the vector's length to 1. why? because in playtesting I found out moving diagonally actually was faster, so this pretty much prevents it.
        movement = Vector3.ClampMagnitude(movement, 1f);

        //this counts down an attack thats happening, will help us know when attack ends.
        if (currentState == PlayerState.Attack)
        {
            attackTimeRemaining = attackTimeRemaining - Time.deltaTime;

            if (attackHitChecked == false)
            {
                attackHitTimeRemaining = attackHitTimeRemaining - Time.deltaTime;

                if (attackHitTimeRemaining <= 0f)
                {
                    attackHitChecked = true;

                    float distance = Vector3.Distance(transform.position, boss.position);

                    if (distance <= attackRange)
                    {
                        bossController.TakeDamage(attackDamage);
                    }
                }
            }
        }

        //this decides what state we want, without changing currentState yet.
        PlayerState nextState = PlayerState.Idle;

        if (movement != Vector3.zero)
        {
            nextState = PlayerState.Move;
        }

        //here, we NEEDED to add that an unfinished attack should take priority over blocking or another attack.
        if (currentState == PlayerState.Charge)
        {
            nextState = PlayerState.Charge;

            //this remembers if space was released, even during the raising motion.
            if (Keyboard.current.spaceKey.isPressed == false)
            {
                chargeReleased = true;
            }

            if (chargeReleased == false)
            {
                chargeTime = chargeTime + Time.deltaTime;

                if (chargeTime >= maxChargeTime && fullChargeShown == false)
                {
                    fullChargeShown = true;
                    fullChargeEffect.Play();
                }
            }

            if (raiseTimeRemaining > 0f)
            {
                raiseTimeRemaining = raiseTimeRemaining - Time.deltaTime;
            }

            //once the sword is raised, either hold the pose or begin the swing.
            if (raiseTimeRemaining <= 0f)
            {
                if (chargeReleased == true)
                {
                    nextState = PlayerState.HeavyAttack;
                }
                else
                {
                    animator.speed = 0f;
                }
            }
        }
        else if (currentState == PlayerState.HeavyAttack)
        {
            heavyAttackTimeRemaining = heavyAttackTimeRemaining - Time.deltaTime;

            if (heavyHitChecked == false)
            {
                heavyHitTimeRemaining = heavyHitTimeRemaining - Time.deltaTime;

                if (heavyHitTimeRemaining <= 0f)
                {
                    heavyHitChecked = true;

                    float distance = Vector3.Distance(transform.position, boss.position);

                    if (distance <= attackRange)
                    {
                        bossController.TakeDamage(chargedDamage);
                    }
                }
            }

            if (heavyAttackTimeRemaining > 0f)
            {
                nextState = PlayerState.HeavyAttack;
            }
        }
        else if (currentState == PlayerState.Attack && attackTimeRemaining > 0f)
        {
            nextState = PlayerState.Attack;
        }
        else if (Mouse.current.rightButton.isPressed && blockCooldownRemaining <= 0f)
        {
            nextState = PlayerState.Block;
        }
        else if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            nextState = PlayerState.Charge;
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            nextState = PlayerState.Attack;
        }

        //applies our final choice once per update.
        SetState(nextState);

        //this was added after feedback, basic attacks should allow movement, but blocking does not.
        if (currentState == PlayerState.Idle ||currentState == PlayerState.Move ||currentState == PlayerState.Attack)
        {
            controller.Move(movement * moveSpeed * Time.deltaTime);
        }
    }
}
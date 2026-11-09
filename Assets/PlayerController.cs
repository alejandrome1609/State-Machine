using Unity.VisualScripting;
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
    public float parryDuration = 0.08f;
    private float parryTimeRemaining = 0f;
    public BossController bossController;
    //damage should happen when the sword swings, not immediately when we click.
    public float attackHitDelay = 0.7f;
    private float attackHitTimeRemaining = 0f;
    private bool attackHitChecked = false;
    public int attackDamage = 10;
    public float attackRange = 6.5f;
    public int heavyBaseDamage = 20;
    public float maxChargeTime = 1.8f;
    //same hit timing idea, but for the heavy attack.
    public float heavyHitDelay = 0.3f;
    private float heavyHitTimeRemaining = 0f;
    private bool heavyHitChecked = false;
    private int chargedDamage = 0;
    public GameManager gameManager;
    //leaving block starts a cooldown so we cant constantly spam new parry windows.
    public float blockCooldown = 0.4f;
    private float blockCooldownRemaining = 0f;
    public ParticleSystem fullChargeEffect;
    private bool fullChargeShown = false;
    public Slider healthBar;
    public Image parryFlash;
    //decided to make normal block protection take a little time to activate, so what when missing the parry window, a player can still take full damage.
    private float parryFlashAlpha = 0f;
    public float blockProtectionDelay = 0.2f;
    private float blockTimeHeld = 0f;

    //this is to check if the boss is close enough, grabs distance between boss and player, and if its greater than the attack range, then nothing happens and vice versa.
    private void HitBoss(int damage)
    {
        float distance = Vector3.Distance(transform.position, boss.position);

        if (distance <= attackRange)
        {
            bossController.TakeDamage(damage);
        }
    }
    //different states our player can be in.
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
    //the boss calls this when its attack reaches the player, which we then decide if that hit gets parried, blocked, or takes full health.
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
            //holding block long enough reduces the damage to 10.
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

    //all state changes go through this function, makes code clean. its also where we start animations and reset the timers each state needs.
    private void SetState(PlayerState newState)
    {
        if (currentState == newState)
        {
            return;
        }
        //this allows leaving block to start a cooldown. rest is pretty self explanatory, just lots of math i had trouble balancing.
        if (currentState == PlayerState.Block)
        {
            blockCooldownRemaining = blockCooldown;
        }
        currentState = newState;
        //returns animation speed to normal before applying any special speed i used below.
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
            //starts counting how long we hold block and opens a fresh parry window
            blockTimeHeld = 0f;
            parryTimeRemaining = parryDuration;
            animator.Play("Block");
        }
        else if (currentState == PlayerState.Attack)
        {
            //the animation plays slower, so dividing by 0.8 makes its timers longer too. added this after balancing since swinging was pretty op, same thing with heavy below
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

            //holding longer than max charge time shouldnt keep adding damage, then you can one tap the thing.
            float countedCharge = chargeTime;

            if (countedCharge > maxChargeTime)
            {
                countedCharge = maxChargeTime;
            }

            //charge amount goes from 0 to 1, one being when its fully charged and 0 when its empty.
            float chargeAmount = countedCharge / maxChargeTime;

            //then a full charge adds 30 damage to our base damage of 20.
            int bonusDamage = Mathf.RoundToInt(chargeAmount * 30f);
            chargedDamage = heavyBaseDamage + bonusDamage;
            animator.speed = 1.5f;
        }
        else if (currentState == PlayerState.Dead)
        {
            //used idle as our placeholder since i couldnt find a death animation anywhere, and I sure as hell wasnt making one, maybe in the future.
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
        //fades the parry flash back to invisible after it appears.
        if (parryFlashAlpha > 0f)
        {
            parryFlashAlpha = parryFlashAlpha - Time.deltaTime * 3f;

            if (parryFlashAlpha < 0f)
            {
                parryFlashAlpha = 0f;
            }

            parryFlash.color = new Color(1f, 1f, 1f, parryFlashAlpha);
        }
        //dead
        if (currentState == PlayerState.Dead)
        {
            return;
        }//only counts held time while we're actually in the block state.
        if (currentState == PlayerState.Block)
        {
            blockTimeHeld = blockTimeHeld + Time.deltaTime;
        }
        if (blockCooldownRemaining > 0f)
        {
            blockCooldownRemaining = blockCooldownRemaining - Time.deltaTime;
        }
        //this pretty much finds the direction towards the boss and ignores the height difference so theres no tilting and funny buisiness going on when fighting.
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

        //here I actually decided to limit the vector's length to 1. why? because in playtesting I found out moving diagonally actually was faster, so this pretty much prevents it.
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
                    HitBoss(attackDamage);
                }
            }
        }

        //idle is our starting choice for this frame unless something below changes it.
        PlayerState nextState = PlayerState.Idle;

        if (movement != Vector3.zero)
        {
            nextState = PlayerState.Move;
        }

        //here, we NEEDED to add that the player must finish a charge or attack before taking another action.
        if (currentState == PlayerState.Charge)
        {
            nextState = PlayerState.Charge;
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
            //same idea as the basic attack, but this hit uses our calculated charge damage.
            if (heavyHitChecked == false)
            {
                heavyHitTimeRemaining = heavyHitTimeRemaining - Time.deltaTime;

                if (heavyHitTimeRemaining <= 0f)
                {
                    heavyHitChecked = true;
                    HitBoss(chargedDamage);
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
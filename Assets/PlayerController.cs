    using UnityEngine;
    using UnityEngine.InputSystem;

    public class PlayerController : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public Animator animator;
        //Since I think I undertstood what enums do last class, I actually went and tried them and for some reason, they just felt better to use than just regular if statements, especially with what im going for here.
        //Made different states a character can have, the starting one being idle.
        private enum PlayerState{
            Idle, Move, Block, Attack
        }
        private PlayerState currentState = PlayerState.Idle;
        public float attackDuration = 1f;
        private float attackTimeRemaining = 0f;
        public CharacterController controller;
        public float moveSpeed = 3f;
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            //This is how attack is going to work, added a timer because this would then allow the attack to fully go uninterrupted for that specific amount of time before accpeting other actions with return at the end there, basically exiting the frame and each other frame until that timer hits 0.
            if (currentState == PlayerState.Attack){
                attackTimeRemaining = attackTimeRemaining - Time.deltaTime;

                if (attackTimeRemaining <= 0){
                    currentState = PlayerState.Idle;
                    animator.Play("Idle");
                }

                return;
            }
            //Blocking sequence, pretty self explanatory, had to group the states together to then get this in english: "Right click must be held and we must be either idle or moving"
            if (Mouse.current.rightButton.isPressed && (currentState == PlayerState.Idle || currentState == PlayerState.Move)){
                currentState = PlayerState.Block;
                animator.Play("Block");
            }
            //This returns the character back to idle after they are done holding block 
            else if (Mouse.current.rightButton.isPressed == false && currentState == PlayerState.Block){
                currentState = PlayerState.Idle;
                animator.Play("Idle");
            }
            //Attacking sequence, kinda the same thing here as block, but this now checks wether it was pressed each frame since attacks will only happen once in the future game im going for.
            //We set the timer again so every new attack get its full duration.
            if (Mouse.current.leftButton.wasPressedThisFrame && (currentState == PlayerState.Idle || currentState == PlayerState.Move)){
                currentState = PlayerState.Attack;
                attackTimeRemaining = attackDuration;
                animator.Play("Attack");
            }

            //Movement code, since my game is just simply moving left or right, it'll just have to be based on directions.
            //Pretty self explanatory, this chunk of code will only work if the player is not attacking or blocking.
            if (currentState == PlayerState.Idle || currentState == PlayerState.Move){
                float direction = 0f;

                if (Keyboard.current.aKey.isPressed){
                    direction = direction - 1f;
                }

                if (Keyboard.current.dKey.isPressed){
                    direction = direction + 1f;
                }

                if (direction != 0f){
                    if (currentState != PlayerState.Move){
                        currentState = PlayerState.Move;
                        animator.Play("Move");
                    }

                    controller.Move(transform.right * direction * moveSpeed * Time.deltaTime);
                    
                }

                else if (currentState == PlayerState.Move){
                    currentState = PlayerState.Idle;
                    animator.Play("Idle");
                }
            }
        }
    }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class Player : MonoBehaviour
{
    public enum PlayerType { PacMan, Ghost}
    [Header("Player")]
    public PlayerType playerType;
    public enum PlayerNumber { PlayerOne, PlayerTwo }
    public PlayerNumber playerNumber;
    public enum PlayerController { Keyboard, Controller }
    public PlayerController playerController;
    public float moveSpeed;
    public bool canMove = true;
    public bool isGhost;
    public bool canControl = true;
    public bool invertedControls = false;
    public Node.ValidDirections startingDirection;
    public Vector2 orientation;
    public Node startingNode;
    public GameObject invulField;
    public int pacManLives;
    [Header("SFX")]
    public AudioClip[] munchSFX;
    public AudioClip superPelletEat;
    [Header ("Playable Ghost")]
    public float rechargeTimeInvertAbility;
    [SerializeField] float currentTimeInvertAbility;
    public bool canInvertAbility;
    [SerializeField] bool invertAbilityActivated;
    public float invertAbilityDuration;
    [SerializeField] float currentInvertAbilityDuration;
    public float rechargeTimeUncontrollableAbility;
    [SerializeField] float currentTimeUncontrollableAbility;
    public bool canUncontrollableAbility;
    [SerializeField] bool uncontrollableAbilityActivated;
    public float uncontrollableAbilityDuration;
    [SerializeField] float currentUncontrollableAbilityDuration;
    public float rechargeTimeCantMoveAbility;
    [SerializeField] float currentTimeCantMoveAbility;
    public bool canCantMoveAbility;
    [SerializeField] bool cantMoveAbilityActivated;
    public float cantMoveAbilityDuration;
    [SerializeField] float currentCantMoveAbilityDuration;
    public RuntimeAnimatorController playerGhostUp;
    public RuntimeAnimatorController playerGhostDown, playerGhostRight, playerGhostLeft,
    playerGhostFright, playerGhostFrightEnd;
    public enum PlayableGhostState { Normal, Frightened, Consumed }
    public PlayableGhostState playableGhostState;
    AudioSource source;
    bool _munch;
    public bool invul;
    bool countInvul;
    float currentInvulTime;
    bool controllerAvailable;
    Node currentNode, targetNode, previousNode;
    SpriteRenderer pacmanSprite;
    GameManager manager;
    InputMaster inputActions;
    Vector2 playerInputVector, nextDirection, gamepadAxis;
    Animator animator;
    Animator ghostPlayerAnim;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
        manager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        pacmanSprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
        source = GetComponent<AudioSource>();
        controllerAvailable = Gamepad.all.Count > 0;

        if (playerNumber == PlayerNumber.PlayerTwo && manager.currentGamemode == GameManager.GameMode.GhostVPlayer)
            ghostPlayerAnim = transform.GetChild(2).GetComponent<Animator>();

        inputActions = new InputMaster();
        inputActions.Enable();
        inputActions.Player.Movement.performed += CheckJoystickInput;
    }

    void Start()
    {
        Node node = GetNodeAtPosition(transform.localPosition);

        if (node != null)
        {
            currentNode = startingNode = node;
        }
        
        playerInputVector = currentNode.ConvertDirectionFromEnum(startingDirection);
        orientation = currentNode.ConvertDirectionFromEnum(startingDirection);
        ChangePosition(playerInputVector);
    }

    void Update()
    {   
        if (canMove)
        {
            Move();

            if (canControl)
                CheckInput();
            
            else
                ChangePosition(Vector2.zero);

            HandleAnimations();

            if (!isGhost)
            {
                EatPellet();
                UpdateRotation();
            }

            else
            {
                CheckPlayerGhostCollisions();
                HandlePlayerGhostAbility();
            }

            if (manager.createdFruit != null && !isGhost)
                CheckCollisions();
            
            if (countInvul)
            {
                if (currentInvulTime >= 5f)
                {
                    invul = false;
                    countInvul = false;
                    currentInvulTime = 0f;
                }

                else
                    currentInvulTime += Time.deltaTime;
            }

            invulField.SetActive(invul);
        }
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        GameObject tile = manager.boardObjects[(int)pos.x, (int)pos.y];

        if (tile != null)
        {
            return tile.GetComponent<Node>();
        }

        return null;
    }

    void CheckCollisions()
    {   
        if (transform.GetChild(0).GetComponent<SpriteRenderer>() != null)
        {
            Rect playerRect = new Rect(transform.position, transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size / 16f);
            Rect fruitRect = new Rect(manager.createdFruit.transform.position, manager.createdFruit.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size / 115f);

            if (playerRect.Overlaps(fruitRect))
            {
                source.PlayOneShot(manager.createdFruit.GetComponent<Fruit>().collectedFruit);

                if (manager.currentGamemode == GameManager.GameMode.Classic || manager.currentGamemode == GameManager.GameMode.TimeTrial)
                    manager.createdFruit.GetComponent<Fruit>().CollectedFruit();
                
                else if (manager.currentGamemode == GameManager.GameMode.PVP2P)
                    manager.createdFruit.GetComponent<Fruit>().SpecificPlayerCollectedFruit(this);
            }
        }
    }

    void CheckPlayerGhostCollisions()
    {
        if (transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().enabled)
        {
            Rect pacmanPlayerRect = new Rect(transform.position, transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size / 16f);
            Rect ghostPlayerRect = new Rect(transform.position, transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size / 16f);

            if (ghostPlayerRect.Overlaps(pacmanPlayerRect))
            {

            }
        }
    }

    void HandlePlayerGhostAbility()
    {
        if (currentTimeInvertAbility >= rechargeTimeInvertAbility && !canInvertAbility)
        {
            canInvertAbility = true;
            currentTimeInvertAbility = 0f;
        }

        else if (currentTimeInvertAbility < rechargeTimeInvertAbility && !canInvertAbility)
            currentTimeInvertAbility += Time.deltaTime;
        
        if (canInvertAbility && invertAbilityActivated)
        {
            if (currentInvertAbilityDuration >= invertAbilityDuration)
            {
                canInvertAbility = false;
                invertAbilityActivated = false;
                currentInvertAbilityDuration = 0f;
                manager.pacMan.InvertPlayerDirections(false);
            }

            else
                currentInvertAbilityDuration += Time.deltaTime;
        }
        
        if (currentTimeCantMoveAbility >= rechargeTimeCantMoveAbility && !canCantMoveAbility)
        {
            canCantMoveAbility = true;
            currentTimeCantMoveAbility = 0f;
        }

        else if (currentTimeCantMoveAbility < rechargeTimeCantMoveAbility && !canCantMoveAbility)
            currentTimeCantMoveAbility += Time.deltaTime;
        
        if (canCantMoveAbility && cantMoveAbilityActivated)
        {
            if (currentCantMoveAbilityDuration >= cantMoveAbilityDuration)
            {
                canCantMoveAbility = false;
                cantMoveAbilityActivated = false;
                currentCantMoveAbilityDuration = 0f;
                manager.pacMan.StopPlayerMovement(false);
            }

            else
                currentCantMoveAbilityDuration += Time.deltaTime;
        }
        
        if (currentTimeUncontrollableAbility >= rechargeTimeUncontrollableAbility && !canUncontrollableAbility)
        {
            canUncontrollableAbility = true;
            currentTimeUncontrollableAbility = 0f;
        }

        else if (currentTimeUncontrollableAbility < rechargeTimeUncontrollableAbility && !canUncontrollableAbility)
            currentTimeUncontrollableAbility += Time.deltaTime;
        
        if (canUncontrollableAbility && uncontrollableAbilityActivated)
        {
            if (currentUncontrollableAbilityDuration >= uncontrollableAbilityDuration)
            {
                canUncontrollableAbility = false;
                uncontrollableAbilityActivated = false;
                currentUncontrollableAbilityDuration = 0f;
                manager.pacMan.UncontrollablePlayer(false);
            }

            else
                currentUncontrollableAbilityDuration += Time.deltaTime;
        }
        
        if (Keyboard.current.gKey.wasPressedThisFrame && canInvertAbility)
        {
            manager.pacMan.InvertPlayerDirections(true);
            invertAbilityActivated = true;
        }

        if (Keyboard.current.hKey.wasPressedThisFrame && canCantMoveAbility)
        {
            manager.pacMan.StopPlayerMovement(true);
            cantMoveAbilityActivated = true;
        }

        if (Keyboard.current.jKey.wasPressedThisFrame && canUncontrollableAbility)
        {
            manager.pacMan.UncontrollablePlayer(true);
            uncontrollableAbilityActivated = true;
        }
    }

    public void InvertPlayerDirections(bool invert)
    {
        if (playerInputVector != Vector2.zero)
        {
            if (targetNode != currentNode && targetNode != null)
            {
                playerInputVector *= -1f;

                Node tempNode = targetNode;
                targetNode = previousNode;
                previousNode = tempNode;
            }
        }

        invertedControls = invert;
    }

    public void StopPlayerMovement(bool _canMove)
    {
        canMove = _canMove;
    }

    public void UncontrollablePlayer(bool _canControl)
    {
        canControl = _canControl;
    }

    void HandleAnimations()
    {   
        if (!isGhost)
            animator.SetBool("moving", (playerInputVector == Vector2.zero) ? false: true);
        
        else
        {
            if (playerInputVector == Vector2.up)
                ghostPlayerAnim.runtimeAnimatorController = playerGhostUp;

            else if (playerInputVector == Vector2.down)
                ghostPlayerAnim.runtimeAnimatorController = playerGhostDown;

            else if (playerInputVector == Vector2.right)
                ghostPlayerAnim.runtimeAnimatorController = playerGhostRight;

            else if (playerInputVector == Vector2.left)
                ghostPlayerAnim.runtimeAnimatorController = playerGhostLeft;
        }
    }

    void Move()
    {
        if (targetNode != currentNode && targetNode != null)
        {
            if (nextDirection == playerInputVector * -1f)
            {
                playerInputVector *= -1f;

                Node tempNode = targetNode;
                targetNode = previousNode;
                previousNode = tempNode;
            }

            if (OverShotTarget())
            {
                currentNode = targetNode;

                transform.localPosition = currentNode.transform.position;

                Transform _portalEnd = GetPortal(currentNode.transform.position);

                if (_portalEnd != null)
                {
                    transform.localPosition = _portalEnd.position;
                    currentNode = _portalEnd.GetComponent<Node>();
                }

                Node moveToNode = CanMove(nextDirection);

                if (moveToNode != null)
                    playerInputVector = nextDirection;
                
                if (moveToNode == null)
                    moveToNode = CanMove(playerInputVector);
                
                if (moveToNode != null)
                {
                    targetNode = moveToNode;
                    previousNode = currentNode;
                    currentNode = null;
                }

                else
                    playerInputVector = Vector2.zero;
            }

            else
                transform.localPosition += (Vector3)playerInputVector * moveSpeed * Time.deltaTime;
        }
    }

    void ChangePosition(Vector2 d)
    {
        if (d != playerInputVector)
            nextDirection = d;
        
        if (currentNode != null)
        {
            if (canControl)
            {
                Node moveToNode = CanMove(d);

                if (moveToNode != null)
                {
                    playerInputVector = d;
                    targetNode = moveToNode;
                    previousNode = currentNode;
                    currentNode = null;
                }
            }
            
            else
            {
                Vector2 _targetNode = GetRandomTile();
                Node moveToNode = null;

                Node[] foundNodes = new Node[4];
                Vector2[] foundNodesDirection = new Vector2[4];

                int nodeCounter = 0;

                for (int i = 0; i < currentNode.neighbours.Count; i++)
                {
                    if (currentNode.ConvertDirectionFromEnum(currentNode.validDirections[i]) != playerInputVector * -1 || GetOpositeDirectionIfCorner(currentNode))
                    {
                        foundNodes[nodeCounter] = currentNode.neighbours[i];
                        foundNodesDirection[nodeCounter] = currentNode.ConvertDirectionFromEnum(currentNode.validDirections[i]);
                        nodeCounter++;
                    }
                }

                if (foundNodes.Length == 1)
                {
                    moveToNode = foundNodes[0];
                    playerInputVector = foundNodesDirection[0];
                }

                else if (foundNodes.Length > 1)
                {
                    float leastDistance = Mathf.Infinity;

                    for (int i = 0; i < foundNodes.Length; i++)
                    {
                        if (foundNodesDirection[i] != Vector2.zero)
                        {
                            float distance = Vector2.Distance(foundNodes[i].transform.position, _targetNode);

                            if (distance < leastDistance)
                            {
                                leastDistance = distance;
                                moveToNode = foundNodes[i];
                                playerInputVector = foundNodesDirection[i];
                            }
                        }
                    }
                }

                if (moveToNode != null)
                {
                    playerInputVector = d;
                    targetNode = moveToNode;
                    previousNode = currentNode;
                    currentNode = null;
                }
            }
        }
    }

    bool GetOpositeDirectionIfCorner(Node _node)
    {
        if (_node.validDirections.Length == 1)
        {
            if (_node.ConvertDirectionFromEnum(_node.validDirections[0]) == playerInputVector * -1)
                return true;
        }

        return false;
    }

    void MoveToNode(Vector2 d)
    {
        Node moveToNode = CanMove(d);

        if (moveToNode != null)
        {
            transform.localPosition = moveToNode.transform.localPosition;
            currentNode = moveToNode;
        }
    }

    Node CanMove(Vector2 d)
    {
        Node moveToNode = null;

        for (int i = 0; i < currentNode.neighbours.Count; i++)
        {   
            if (currentNode.ConvertDirectionFromEnum(currentNode.validDirections[i]) == d)
            {
                moveToNode = currentNode.neighbours[i];
                break;
            }
        }

        return moveToNode;
    }

    void UpdateRotation()
    {
        if (playerInputVector == Vector2.right)
        {
            pacmanSprite.flipX = false;
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        
        else if (playerInputVector == Vector2.left)
        {
            pacmanSprite.flipX = true;
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        
        else if (playerInputVector == Vector2.up)
        {
            pacmanSprite.flipX = false;
            transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        else if (playerInputVector == Vector2.down)
        {
            pacmanSprite.flipX = false;
            transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }

        orientation = playerInputVector;
    }

    void CheckInput()
    {   
        if (playerController != PlayerController.Controller)
        {   
            if (playerNumber == PlayerNumber.PlayerOne)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                    ChangePosition( invertedControls ? Vector2.right : Vector2.left );
                
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    ChangePosition( invertedControls ? Vector2.left : Vector2.right );
                
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                    ChangePosition( invertedControls ? Vector2.up : Vector2.down );
                
                else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                    ChangePosition( invertedControls ? Vector2.down : Vector2.up );
            }

            else
            {
                if (Keyboard.current.aKey.wasPressedThisFrame)
                    ChangePosition(Vector2.left);
                
                else if (Keyboard.current.dKey.wasPressedThisFrame)
                    ChangePosition(Vector2.right);
                
                else if (Keyboard.current.sKey.wasPressedThisFrame)
                    ChangePosition(Vector2.down);
                
                else if (Keyboard.current.wKey.wasPressedThisFrame)
                    ChangePosition(Vector2.up);
            }
        }

        else
        {
            if (playerNumber == PlayerNumber.PlayerOne)
            {
                if (Gamepad.all[manager.controllerOneIndexToUse].dpad.left.wasPressedThisFrame)
                    ChangePosition( invertedControls ? Vector2.right : Vector2.left );
                
                else if (Gamepad.all[manager.controllerOneIndexToUse].dpad.right.wasPressedThisFrame)
                    ChangePosition( invertedControls ? Vector2.left : Vector2.right );
                
                else if (Gamepad.all[manager.controllerOneIndexToUse].dpad.down.wasPressedThisFrame)
                    ChangePosition( invertedControls ? Vector2.up : Vector2.down );
                
                else if (Gamepad.all[manager.controllerOneIndexToUse].dpad.up.wasPressedThisFrame)
                    ChangePosition( invertedControls ? Vector2.down : Vector2.up );
            }

            else
            {
                if (Gamepad.all[manager.controllerTwoIndexToUse].dpad.left.wasPressedThisFrame)
                    ChangePosition(Vector2.left);
                
                else if (Gamepad.all[manager.controllerTwoIndexToUse].dpad.right.wasPressedThisFrame)
                    ChangePosition(Vector2.right);
                
                else if (Gamepad.all[manager.controllerTwoIndexToUse].dpad.down.wasPressedThisFrame)
                    ChangePosition(Vector2.down);
                
                else if (Gamepad.all[manager.controllerTwoIndexToUse].dpad.up.wasPressedThisFrame)
                    ChangePosition(Vector2.up);
            }
        }
    }

    void CheckJoystickInput(InputAction.CallbackContext context)
    {   
        /*if (playerController == PlayerController.Controller)
        {
            gamepadAxis = context.ReadValue<Vector2>();

            if (gamepadAxis == Vector2.left)
                ChangePosition(Vector2.left);
                
            else if (gamepadAxis == Vector2.right)
                ChangePosition(Vector2.right);
                
            else if (gamepadAxis == Vector2.down)
                ChangePosition(Vector2.down);
                
            else if (gamepadAxis == Vector2.up)
                ChangePosition(Vector2.up);
        }*/
    }

    GameObject GetPelletAtPosition(Vector2 pos)
    {
        int tileX = Mathf.RoundToInt(pos.x);
        int tileY = Mathf.RoundToInt(pos.y);

        GameObject pellet = manager.boardObjects[tileX, tileY];

        if (pellet != null)
            return pellet;
        
        return null;
    }

    void EatPellet()
    {
        GameObject pellet = GetPelletAtPosition(transform.localPosition);

        if (pellet != null)
        {
            if (!pellet.GetComponent<Node>().eaten && !pellet.GetComponent<Node>().invisiblePellet && pellet.GetComponent<Node>().pelletType != Node.PelletType.SuperPellet)
            {
                pellet.GetComponent<SpriteRenderer>().enabled = false;
                pellet.GetComponent<Node>().eaten = true;

                if (playerNumber == PlayerNumber.PlayerOne)
                {
                    manager.score += pellet.GetComponent<Node>().scoreValue;

                    if (manager.currentGamemode == GameManager.GameMode.Classic)
                        manager.eatenPellets++;
                    
                    else if (manager.currentGamemode == GameManager.GameMode.PVP2P)
                        manager.p1EatenPellets[manager.currentActIndex]++;
                    
                    else if (manager.currentGamemode == GameManager.GameMode.TimeTrial)
                    {
                        manager.timeTrialPelletCount++;
                        manager.eatenPellets++;

                        if (manager.timeTrialPelletCount >= manager.pelletsToAddTime)
                        {
                            manager.AddTimeTrialTime("pellet");
                            manager.timeTrialPelletCount = 0;
                        }
                    }
                }
                
                else
                {
                    manager.p2Score += pellet.GetComponent<Node>().scoreValue;
                    manager.p2EatenPellets[manager.currentActIndex]++;
                }

                if (manager.currentGamemode != GameManager.GameMode.TimeTrial)
                {
                    manager.pelletFruitCounter++;
                    manager.CheckToAppearFruit();
                }

                manager.CheckForSirenChange();
                source.PlayOneShot(_munch ? munchSFX[1] : munchSFX[0]);
                _munch = !_munch;
            }

            else if (!pellet.GetComponent<Node>().eaten && !pellet.GetComponent<Node>().invisiblePellet && pellet.GetComponent<Node>().pelletType == Node.PelletType.SuperPellet)
            {
                //Super Pellet
                pellet.GetComponent<SpriteRenderer>().enabled = false;
                pellet.GetComponent<Node>().eaten = true;

                if (playerNumber == PlayerNumber.PlayerOne)
                {
                    manager.score += pellet.GetComponent<Node>().scoreValue;

                    if (manager.currentGamemode == GameManager.GameMode.Classic || manager.currentGamemode == GameManager.GameMode.TimeTrial)
                        manager.eatenPellets++;
                    
                    else
                        manager.p1EatenPellets[manager.currentActIndex]++;
                }
                
                else
                {
                    manager.p2Score += pellet.GetComponent<Node>().scoreValue;
                    manager.p2EatenPellets[manager.currentActIndex]++;
                }

                source.PlayOneShot(superPelletEat);
                manager.AddTimeTrialTime("superpellet");

                if (manager.currentGamemode != GameManager.GameMode.TimeTrial)
                {
                    manager.pelletFruitCounter++;
                    manager.CheckToAppearFruit();
                }

                manager.CheckForSirenChange();
                manager.TriggerSuperPellet();
            }
        }
    }

    Transform GetPortal(Vector2 pos)
    {
        GameObject pellet = manager.boardObjects[(int)pos.x, (int)pos.y];

        if (pellet != null && pellet.GetComponent<Node>() != null)
        {
            if (pellet.GetComponent<Node>().isPortal)
            {
                return pellet.GetComponent<Node>().portalEnd;
            }
        }

        return null;
    }

    Vector2 GetRandomTile()
    {
        int x = Random.Range(0, GameManager.boardWidth);
        int y = Random.Range(0, GameManager.boardHeight);

        return new Vector2(x, y);
    }

    public void Restart()
    {
        canMove = true;
        animator.SetTrigger("reappear");
        transform.position = startingNode.transform.position;
        currentNode = startingNode;
        playerInputVector = orientation = nextDirection = currentNode.ConvertDirectionFromEnum(startingDirection);
        ChangePosition(playerInputVector);
    }

    public void RestartWithInvul()
    {
        canMove = true;
        countInvul = true;
        animator.SetTrigger("reappear");
        transform.position = startingNode.transform.position;
        currentNode = startingNode;
        playerInputVector = orientation = nextDirection = currentNode.ConvertDirectionFromEnum(startingDirection);
        ChangePosition(playerInputVector);
    }

    float LengthFromNode(Vector2 targetPosition)
    {
        Vector2 vec = targetPosition - (Vector2)previousNode.transform.position;
        return vec.sqrMagnitude;
    }

    bool OverShotTarget()
    {
        float nodeToTarget = LengthFromNode(targetNode.transform.position);
        float nodeToSelf = LengthFromNode(transform.localPosition);

        return nodeToSelf > nodeToTarget;
    }
}

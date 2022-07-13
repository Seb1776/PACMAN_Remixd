using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    [Header ("Enemy")]
    public float moveSpeed;
    public float consumedMoveSpeed;
    public bool canMove = true;
    public Node startingNode;
    public Node scatterNode;
    public Node houseNode;
    public int[] scatterModeTimes;
    public int[] chaseModeTimes;
    public float timeToReleaseGhost;
    public bool startsInGhostHouse;
    public bool ghostInHouse;
    public int maxModeChangeIteration;
    public enum GhostMode { Chase, Scatter, Frightened, Consumed }
    public GhostMode ghostMode;
    public enum GhostAIMode { FollowPacman, TilesAhead, ChaseAndRetreat, TilesTeamBased }
    public GhostAI ghostAI;
    public Node.ValidDirections startingDirection;
    public Transform target;
    [Header ("Animations")]
    public RuntimeAnimatorController ghostUp;
    public RuntimeAnimatorController ghostDown, ghostRight, ghostLeft, frightStart, frightEnd,
    eyesUp, eyesDown, eyesRight, eyesLeft;
    [Header ("Audio")]
    public AudioClip consumeSFX;
    [Header ("UI")]
    public TMP_Text scoreConsumeText;

    [Header("Debug")]
    public bool drawPathGizmos;
    public Animator animator;

    AudioSource source;
    float currentMoveSpeed;
    bool finalIteration;
    Node currentNode, targetNode, previousNode;
    Vector2 enemyDirection, nextDirection;
    GhostMode previousMode;
    GameManager manager;
    int modeChangeIteration;
    float ghostReleaseTimer;
    float modeChangeTimer = 0f;

    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        animator = transform.GetChild(0).GetComponent<Animator>();
        source = GetComponent<AudioSource>();

        Node node = GetNodeAtPosition(transform.position);

        if (node != null)
        {
            startingNode = node;
            currentNode = node;
        }
        
        if (ghostInHouse)
        {
            enemyDirection = Vector2.up;
            targetNode = currentNode.neighbours[0];
        }

        else
        {
            enemyDirection = currentNode.ConvertDirectionFromEnum(startingDirection);
            targetNode = ChooseNextNode();
        }

        HandleAnimations();
        
        previousNode = currentNode;

        currentMoveSpeed = moveSpeed;
    }

    void Update()
    {   
        if (canMove)
        {
            Move();
            UpdateMode();

            if (ghostInHouse)
                ReleaseGhost();
            
            CheckCollisions();
            CheckIsInHouse();
            CheckPreferredTarget();
        }
    }

    void CheckCollisions()
    {
        Rect pacMan2Rect = new Rect(Vector2.zero, Vector2.zero);
        Rect pacManRect = new Rect(Vector2.zero, Vector2.zero);
        Rect ghostRect = new Rect(transform.position, transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size /16f);

        if (manager.pacMan2 != null)
        {
            if (manager.pacMan.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled && !manager.pacMan.invul)
                pacManRect = new Rect(manager.pacMan.transform.position, manager.pacMan.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size / 16f);
            
            if (manager.pacMan2.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled && !manager.pacMan2.invul)
                pacMan2Rect = new Rect(manager.pacMan2.transform.position, manager.pacMan2.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size / 16f);

            if (ghostRect.Overlaps(pacMan2Rect))
            {
                if (ghostMode == GhostMode.Frightened)
                    ConsumedGhost(manager.pacMan2);
                
                else if ((ghostMode != GhostMode.Frightened && ghostMode != GhostMode.Consumed) && !manager.pacMan2.invul)
                {
                    manager.ghostKilledName = gameObject.name.Replace("(Clone)", string.Empty).ToLower();
                    manager.StartDeath(manager.pacMan2);
                }
            }

            if (ghostRect.Overlaps(pacManRect) && manager.pacMan.transform.GetChild(0).GetComponent<SpriteRenderer>() != null)
            {
                if (ghostMode == GhostMode.Frightened)
                    ConsumedGhost(manager.pacMan);
                
                else if ((ghostMode != GhostMode.Frightened && ghostMode != GhostMode.Consumed) && !manager.pacMan.invul)
                {
                    manager.ghostKilledName = gameObject.name.Replace("(Clone)", string.Empty).ToLower();
                    manager.StartDeath(manager.pacMan);
                }
            }
        }

        else
        {
            pacManRect = new Rect(manager.pacMan.transform.position, manager.pacMan.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size / 16f);

            if (ghostRect.Overlaps(pacManRect))
            {
                if (ghostMode == GhostMode.Frightened)
                    ConsumedGhost();
                
                else if (ghostMode != GhostMode.Frightened && ghostMode != GhostMode.Consumed)
                {
                    manager.ghostKilledName = gameObject.name.Replace("(Clone)", string.Empty).ToLower();
                    manager.StartDeath();
                }
            }
        }
    }

    void CheckPreferredTarget()
    {
        if (target != manager.GetPreferredTarget() && (manager.currentGamemode != GameManager.GameMode.Classic || manager.currentGamemode != GameManager.GameMode.TimeTrial))
        {
            target = manager.GetPreferredTarget();
        }
    }

    void CheckIsInHouse()
    {
        if (ghostMode == GhostMode.Consumed)
        {
            GameObject tile = GetTileAtPosition(transform.position);

            if (tile != null)
            {
                if (tile.GetComponent<Node>().isHouseEntrance)
                {
                    Node node = GetNodeAtPosition(transform.position);

                    if (node != null)
                    {
                        currentNode = node;
                        enemyDirection = Vector2.down;
                        targetNode = currentNode.neighbours[0];
                        previousNode = currentNode;
                        HandleAnimations();
                    }
                }

                else if (tile.GetComponent<Node>().isHouse)
                {
                    currentMoveSpeed = moveSpeed;
                    
                    Node node = GetNodeAtPosition(transform.position);

                    if (node != null)
                    {
                        currentNode = node;
                        enemyDirection = Vector2.up;
                        targetNode = currentNode.neighbours[0];
                        previousNode = currentNode;
                        manager.consumedGhosts--;
                        manager.CheckForConsumedGhosts(false);
                        ChangeGhostMode(previousMode);
                        HandleAnimations();
                    }
                }
            }
        }
    }

    IEnumerator ConsumeGhostDelay(float delay, int ghostScore)
    {
        manager.pacMan.canMove = false;

        if (manager.pacMan2 != null) manager.pacMan2.canMove = false;

        manager.blinky.canMove = manager.pinky.canMove = manager.inky.canMove = manager.clyde.canMove = false;
        manager.musicSource.Pause();
        manager.countTime = false;
        transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;
        manager.pacMan.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;

        if (manager.pacMan.invul)
            manager.pacMan.transform.GetChild(1).gameObject.SetActive(false);
        
        if (manager.pacMan2 != null && manager.pacMan2.invul)
            manager.pacMan2.transform.GetChild(1).gameObject.SetActive(false);

        if (manager.pacMan2 != null) manager.pacMan2.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;
        scoreConsumeText.text = ghostScore.ToString();

        yield return new WaitForSeconds(delay);

        scoreConsumeText.text = string.Empty;
        manager.pacMan.canMove = true;

        if (manager.pacMan2 != null) manager.pacMan2.canMove = true;

        manager.countTime = true;
        manager.blinky.canMove = manager.pinky.canMove = manager.inky.canMove = manager.clyde.canMove = true;
        manager.musicSource.Play();
        transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
        manager.pacMan.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
        if (manager.pacMan2 != null) manager.pacMan2.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;

        if (manager.pacMan.invul)
            manager.pacMan.transform.GetChild(1).gameObject.SetActive(true);
        
        if (manager.pacMan2 != null && manager.pacMan2.invul)
            manager.pacMan2.transform.GetChild(1).gameObject.SetActive(true);

        HandleAnimations();
    }

    void ConsumedGhost(Player eatedBy = null)
    {   
        if (eatedBy == null || eatedBy == manager.pacMan)
        {
            manager.consumedGhosts++;
            manager.ghostToConsume--;
            manager.score += manager.lastGhostScore;
            manager.CheckForConsumedGhosts(true);
            source.PlayOneShot(consumeSFX);
            ghostMode = GhostMode.Consumed;
            currentMoveSpeed = consumedMoveSpeed;
            StartCoroutine(ConsumeGhostDelay(consumeSFX.length, manager.lastGhostScore));
            manager.lastGhostScore += manager.lastGhostScore;

            if (manager.currentGamemode == GameManager.GameMode.TimeTrial)
                manager.AddTimeTrialTime("ghost");
        }

        else if (eatedBy == manager.pacMan2)
        {
            manager.consumedGhosts++;
            manager.ghostToConsume--;
            manager.p2Score += manager.p2LastGhostScore;
            manager.CheckForConsumedGhosts(true);
            source.PlayOneShot(consumeSFX);
            ghostMode = GhostMode.Consumed;
            currentMoveSpeed = consumedMoveSpeed;
            StartCoroutine(ConsumeGhostDelay(consumeSFX.length, manager.p2LastGhostScore));
            manager.p2LastGhostScore += manager.p2LastGhostScore;
        }
    }

    public void Restart()
    {
        canMove = true;
        transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
        
        if (modeChangeIteration < maxModeChangeIteration)
            ghostMode = GhostMode.Scatter;
        
        else
            ghostMode = GhostMode.Chase;
    
        modeChangeTimer = 0f;
        finalIteration = false;
        transform.position = startingNode.transform.position;
        ghostReleaseTimer = 0f;

        currentNode = startingNode;

        if (startsInGhostHouse)
        {
            ghostInHouse = true;
            enemyDirection = Vector2.up;
            targetNode = currentNode.neighbours[0];
        }

        else
        {
            enemyDirection = Vector2.left;
            targetNode = ChooseNextNode();
        }

        previousNode = currentNode;
        HandleAnimations();
    }

    void Move()
    {
        if (targetNode != currentNode && targetNode != null && !ghostInHouse)
        {
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

                targetNode = ChooseNextNode();
                previousNode = currentNode;
                currentNode = null;

                if (ghostMode != GhostMode.Frightened)
                    HandleAnimations();
            }

            else
            {
                transform.localPosition += (Vector3)enemyDirection * currentMoveSpeed * Time.deltaTime;
            }
            
            if (drawPathGizmos)
            {
                Debug.DrawLine(transform.position, targetNode.transform.position, Color.yellow);
                Debug.DrawLine(transform.position, target.position, Color.green);
            }
        }
    }

    void UpdateMode()
    {
        if (ghostMode != GhostMode.Frightened)
        {   
            if (!finalIteration)
            {
                modeChangeTimer += Time.deltaTime;

                if (ghostMode == GhostMode.Scatter && modeChangeTimer > scatterModeTimes[modeChangeIteration])
                {
                    ChangeGhostMode(GhostMode.Chase);
                    modeChangeTimer = 0f;
                }

                if (ghostMode == GhostMode.Chase && modeChangeTimer > chaseModeTimes[modeChangeIteration])
                {
                    if (modeChangeIteration >= maxModeChangeIteration)
                        finalIteration = true;
                    
                    else
                    {
                        ChangeGhostMode(GhostMode.Scatter);
                        modeChangeTimer = 0f;
                        modeChangeIteration++;
                    }
                }
            }
        }
    }

    void HandleAnimations()
    {  
        if (ghostMode != GhostMode.Consumed)
        { 
            if (enemyDirection == Vector2.left)
                animator.runtimeAnimatorController = ghostLeft;

            else if (enemyDirection == Vector2.right)
                animator.runtimeAnimatorController = ghostRight;

            else if (enemyDirection == Vector2.up)
                animator.runtimeAnimatorController = ghostUp;

            else if (enemyDirection == Vector2.down)
                animator.runtimeAnimatorController = ghostDown;
        }

        else if (ghostMode == GhostMode.Consumed)
        {
            if (enemyDirection == Vector2.left)
                animator.runtimeAnimatorController = eyesLeft;

            else if (enemyDirection == Vector2.right)
                animator.runtimeAnimatorController = eyesRight;

            else if (enemyDirection == Vector2.up)
                animator.runtimeAnimatorController = eyesUp;

            else if (enemyDirection == Vector2.down)
                animator.runtimeAnimatorController = eyesDown;
        }
    }

    void ChangeGhostMode(GhostMode g)
    {   
        if (ghostMode != g)
        {
            previousMode = ghostMode;
            ghostMode = g;
        }

        HandleAnimations();
    }

    public void StartFrightenedMode()
    {
        currentMoveSpeed = moveSpeed / 2f;
        ChangeGhostMode(GhostMode.Frightened);
        animator.runtimeAnimatorController = frightStart;
    }

    public void StopFrightenedMode()
    {
        currentMoveSpeed = moveSpeed;
        ChangeGhostMode(previousMode);
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        GameObject tile = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().boardObjects[(int)pos.x, (int)pos.y];

        if (tile != null)
        {
            return tile.GetComponent<Node>();
        }

        return null;
    }

    bool GetOpositeDirectionIfCorner(Node _node)
    {
        if (_node.validDirections.Length == 1)
        {
            if (_node.ConvertDirectionFromEnum(_node.validDirections[0]) == enemyDirection * -1)
                return true;
        }

        return false;
    }

    void ReleaseGhost()
    {
        if (ghostReleaseTimer >= timeToReleaseGhost)
            ghostInHouse = false;

        else
            ghostReleaseTimer += Time.deltaTime;
    }

    GameObject GetTileAtPosition(Vector2 pos)
    {
        int tileX = Mathf.RoundToInt(pos.x);
        int tileY = Mathf.RoundToInt(pos.y);

        GameObject pellet = manager.boardObjects[tileX, tileY];

        if (pellet != null)
            return pellet;
        
        return null;
    }

    Vector2 GetRandomTile()
    {
        int x = Random.Range(0, GameManager.boardWidth);
        int y = Random.Range(0, GameManager.boardHeight);

        return new Vector2(x, y);
    }

    Vector2 FollowPacmanBehaviour()
    {
        Vector2 targetPosition = target.position;
        Vector2 targetNode = new Vector2(Mathf.RoundToInt(targetPosition.x), Mathf.RoundToInt(targetPosition.y));

        return targetNode;
    }

    Vector2 TilesAheadBehaviour()
    {
        Vector2 targetPosition = target.position;
        Vector2 targetOrientation = target.GetComponent<Player>().orientation;

        int targetPositionX = Mathf.RoundToInt(targetPosition.x);
        int targetPositionY = Mathf.RoundToInt(targetPosition.y);

        Vector2 pacmanTile = new Vector2(targetPositionX, targetPositionY);
        Vector2 targetTile = pacmanTile + (ghostAI.offsetTiles * targetOrientation);

        return targetTile;
    }

    Vector2 ChaseAndRetreatBehaviour()
    {
        Vector2 pacManPosition = target.transform.localPosition;
        float distance = GetDistance(transform.localPosition, pacManPosition);
        Vector2 targetTile = Vector2.zero;

        if (distance > ghostAI.tilesToChaseToRetreat.x)
        {
            targetTile = new Vector2(Mathf.RoundToInt(pacManPosition.x), Mathf.RoundToInt(pacManPosition.y));
            currentMoveSpeed = moveSpeed;
        }

        else if (distance < ghostAI.tilesToChaseToRetreat.y)
        {
            targetTile = scatterNode.transform.position;
            currentMoveSpeed = moveSpeed;
        }

        return targetTile;
    }

    Vector2 TilesTeamBasedBehaviour()
    {
        Vector2 targetPosition = target.position;
        Vector2 targetOrientation = target.GetComponent<Player>().orientation;

        int targetPositionX = Mathf.RoundToInt(targetPosition.x);
        int targetPositionY = Mathf.RoundToInt(targetPosition.y);

        Vector2 pacmanTile = new Vector2(targetPositionX, targetPositionY);
        Vector2 targetTile = pacmanTile + (ghostAI.offsetTiles * targetOrientation);

        Vector2 tempGhostPos = FindGhostWithName(ghostAI.ghostNameToDepend).transform.position;

        int ghostPositionX = Mathf.RoundToInt(tempGhostPos.x);
        int ghostPositionY = Mathf.RoundToInt(tempGhostPos.y);

        tempGhostPos = new Vector2(ghostPositionX, ghostPositionY);

        float distance = GetDistance(tempGhostPos, targetTile);
        distance *= 2f;

        return new Vector2(tempGhostPos.x + distance, tempGhostPos.y + distance);
    }

    Enemy FindGhostWithName(string ghostName)
    {
        switch (ghostName)
        {
            case "blinky":
                return manager.blinky;

            case "inky":
                return manager.inky;

            case "pinky":
                return manager.pinky;

            case "clyde":
                return manager.clyde;
        }

        return null;
    }

    Vector2 GetTargetTile()
    {
        Vector2 targetPellet = Vector2.zero;

        switch (ghostAI.ghostAIMode)
        {
            case GhostAIMode.FollowPacman:
                targetPellet = FollowPacmanBehaviour();
            break;

            case GhostAIMode.TilesAhead:
                targetPellet = TilesAheadBehaviour();
            break;

            case GhostAIMode.ChaseAndRetreat:
                targetPellet = ChaseAndRetreatBehaviour();
            break;

            case GhostAIMode.TilesTeamBased:
                targetPellet = TilesTeamBasedBehaviour();
            break;
        }

        return targetPellet;
    }

    Node ChooseNextNode()
    {
        Vector2 targetNode = Vector2.zero;

        switch (ghostMode)
        {
            case GhostMode.Chase:
                targetNode = GetTargetTile();
            break;

            case GhostMode.Scatter:
                targetNode = scatterNode.transform.position;
                currentMoveSpeed = moveSpeed;
            break;

            case GhostMode.Frightened:
                targetNode = GetRandomTile();
            break;

            case GhostMode.Consumed:
                targetNode = houseNode.transform.position;
            break;
        }

        Node moveToNode = null;

        Node[] foundNodes = new Node[4];
        Vector2[] foundNodesDirection = new Vector2[4];

        int nodeCounter = 0;

        for (int i = 0; i < currentNode.neighbours.Count; i++)
        {
            if (currentNode.ConvertDirectionFromEnum(currentNode.validDirections[i]) != enemyDirection * -1 || GetOpositeDirectionIfCorner(currentNode))
            {
                foundNodes[nodeCounter] = currentNode.neighbours[i];
                foundNodesDirection[nodeCounter] = currentNode.ConvertDirectionFromEnum(currentNode.validDirections[i]);
                nodeCounter++;
            }
        }

        if (foundNodes.Length == 1)
        {
            moveToNode = foundNodes[0];
            enemyDirection = foundNodesDirection[0];
        }

        else if (foundNodes.Length > 1)
        {
            float leastDistance = Mathf.Infinity;

            for (int i = 0; i < foundNodes.Length; i++)
            {
                if (foundNodesDirection[i] != Vector2.zero)
                {
                    float distance = GetDistance(foundNodes[i].transform.position, targetNode);

                    if (distance < leastDistance)
                    {
                        leastDistance = distance;
                        moveToNode = foundNodes[i];
                        enemyDirection = foundNodesDirection[i];
                    }
                }
            }
        }

        return moveToNode;
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

    float LengthFromNode(Vector2 targetPosition)
    {
        Vector2 vec = targetPosition - (Vector2)previousNode.transform.position;
        return vec.sqrMagnitude;
    }

    float GetDistance(Vector2 posA, Vector2 posB)
    {
        float dx = posA.x - posB.x;
        float dy = posA.y - posB.y;

        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    bool OverShotTarget()
    {
        float nodeToTarget = LengthFromNode(targetNode.transform.position);
        float nodeToSelf = LengthFromNode(transform.localPosition);

        return nodeToSelf > nodeToTarget;
    }
}

[System.Serializable]
public class GhostAI
{
    public Enemy.GhostAIMode ghostAIMode;
    [Header("Tiles Ahead")]
    public int offsetTiles;
    [Header("Chase And Retreat")]
    public Vector2 tilesToChaseToRetreat;
    public float chaseSpeed;
    public float retreatSpeed;
    [Header("TilesTeamBased")]
    public string ghostNameToDepend;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public List<Node> neighbours = new List<Node>();
    public enum ValidDirections { Up, Down, Right, Left, Nil }
    public ValidDirections[] validDirections;
    public enum PelletType { CornerPellet, NormalPellet, SuperPellet }
    public PelletType pelletType;
    public GameObject pelletPrefab;
    public Transform pelletsParent;
    public bool manualNeighbours;
    public bool eaten;
    public int scoreValue;
    public bool invisiblePellet;
    public bool detectableForOtherPellets = true;
    public bool setAsSuperPellet;
    public bool isHouseEntrance;
    public bool isHouse;
    public List<DirectionFromDistance> directionFromDistances = new List<DirectionFromDistance>();
    public List<Vector2> newPelletsSpawns = new List<Vector2>();
    [Header ("Portal Pellet")]
    public bool isPortal;
    public Transform portalEnd;
    [Header("Debug")]
    public bool showDebugGizmos;
    public float raycastDetectLength;
    public float detectNodeRadius;
    public LayerMask whatIsNode;
    public LayerMask circleNode;

    GameManager manager;

    void Start()
    {   
        manager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        GetComponent<SpriteRenderer>().enabled = invisiblePellet ? false : true;
    }

    public void GetNeighbours(GameManager _manager)
    {
        manager = _manager;

        if (!manualNeighbours)
        {
            Vector2[] directs = new Vector2[4];

            directs[0] = Vector2.up;
            directs[1] = Vector2.down;
            directs[2] = Vector2.right;
            directs[3] = Vector2.left;

            for (int i = 0; i < directs.Length; i++)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directs[i], raycastDetectLength, whatIsNode);

                if (hit.collider != null && hit.collider.GetComponent<Node>() != null && hit.collider.GetComponent<Node>().detectableForOtherPellets)
                {   
                    if (!isPortal || (isPortal && !hit.collider.GetComponent<Node>().isPortal))
                        neighbours.Add(hit.collider.GetComponent<Node>());
                }
            }

            GetValidDirections();
        }

        else if (manualNeighbours && neighbours.Count > 0)
        {
            GetValidDirections();
        }
    }

    void GetValidDirections()
    {
        validDirections = new ValidDirections[neighbours.Count];

        for (int i = 0; i < neighbours.Count; i++)
        {
            Node neighbor = neighbours[i];
            Vector2 tempVector = neighbor.transform.position - transform.position;
            validDirections[i] = ConvertDirectionFromVector(tempVector.normalized);
        }

        SetNormalPellets();
    }

    void SetNormalPellets()
    {
        for (int i = 0; i < neighbours.Count; i++)
        {   
            if (!neighbours[i].invisiblePellet && !invisiblePellet)
            {  
                float distanceBtwNeighbour = Vector2.Distance(transform.position, neighbours[i].transform.position) - 1f;
        
                DirectionFromDistance dfd = new DirectionFromDistance(distanceBtwNeighbour, validDirections[i]);
                directionFromDistances.Add(dfd);
            }
        }

        for (int i = 0; i < directionFromDistances.Count; i++)
        {   
            newPelletsSpawns.Add((Vector2)transform.position + ConvertDirectionFromEnum(directionFromDistances[i].direction));

            for (int j = 0; j < directionFromDistances[i].distance; j++)
            {
                newPelletsSpawns.Add(newPelletsSpawns[newPelletsSpawns.Count - 1] + ConvertDirectionFromEnum(directionFromDistances[i].direction));
            }
        }

        if (newPelletsSpawns.Count > 0)
        {
            for (int i = 0; i < newPelletsSpawns.Count; i++)
            {   
                Collider2D detect = Physics2D.OverlapCircle(newPelletsSpawns[i], detectNodeRadius, circleNode);

                if (detect == null)
                {
                    GameObject _node = Instantiate(pelletPrefab, newPelletsSpawns[i], Quaternion.identity, pelletsParent);
                    _node.GetComponent<Node>().pelletType = Node.PelletType.NormalPellet;
                    _node.gameObject.name += " " + i.ToString();
                    manager.allNodes.Add(_node.GetComponent<Node>());
                    manager.totalPellets++;
                }
            }
        }

        if (setAsSuperPellet)
        {
            scoreValue = 50;
            pelletType = PelletType.SuperPellet;
        }
    }

    public Vector2 ConvertDirectionFromEnum(ValidDirections _validDirections)
    {
        switch (_validDirections)
        {
            case ValidDirections.Up:
                return new Vector2(0f, 1f);
            
            case ValidDirections.Down:
                return new Vector2(0f, -1f);
            
            case ValidDirections.Right:
                return new Vector2(1f, 0f);
            
            case ValidDirections.Left:
                return new Vector2(-1f, 0f);
        }

        return Vector2.zero;
    }

    public ValidDirections ConvertDirectionFromVector(Vector2 _dir)
    {
        if (_dir.x == 1f && _dir.y == 0f)
            return ValidDirections.Right;
        
        else if (_dir.x == -1f && _dir.y == 0f)
            return ValidDirections.Left;
        
        else if (_dir.x == 0f && _dir.y == 1f)
            return ValidDirections.Up;
        
        else if (_dir.x == 0f && _dir.y == -1f)
            return ValidDirections.Down;
        
        Debug.Log("why " + gameObject.name + " x: " + _dir.x + ", y: " + _dir.y);
        return ValidDirections.Nil;
    }

    void OnDrawGizmosSelected()
    {   
        if (showDebugGizmos)
        {
            Vector2[] directs = new Vector2[4];

            directs[0] = Vector2.up;
            directs[1] = Vector2.down;
            directs[2] = Vector2.right;
            directs[3] = Vector2.left;

            for (int i = 0; i < directs.Length; i++)
            {
                Debug.DrawRay(transform.position, directs[i] * raycastDetectLength, Color.red);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectNodeRadius);
        }
    }
}

[System.Serializable]
public class DirectionFromDistance
{
    public float distance;
    public Node.ValidDirections direction;

    public DirectionFromDistance(float distance, Node.ValidDirections direction)
    {
        this.distance = distance;
        this.direction = direction;
    }
}

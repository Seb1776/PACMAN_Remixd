using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Maze Data", menuName = "Maze/Create Maze Data")]
public class Maze : ScriptableObject 
{
    public string mazeName;
    public MazeAct[] mazeActs;
    public GameObject tilemap, pelletsParent;
    public Sprite levelPreview;
    public int pelletsToAppearFruit;
}

[System.Serializable]
public class MazeAct
{
    public List<Fruit> fruitsToAppear = new List<Fruit>();
}

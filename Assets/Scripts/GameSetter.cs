using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSetter : MonoBehaviour
{
    public DifficultySetting selectedDifficulty;
    public GameManager.GameMode selectedMode;
    public string selectedMaze;
    public Color p1Color;
    public Color p2Color;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}

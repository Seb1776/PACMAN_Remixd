using UnityEngine;

[CreateAssetMenu(fileName = "New Difficulty Setting", menuName = "Difficulty Settings/Create Difficulty Setting", order = 0)]
public class DifficultySetting : ScriptableObject 
{
    public string difficultyName;
    public GameManager.MusicMode difficultyMusicMode;
    public AudioClip customMusic;
    public GhostConfigs[] ghostConfigs;
    public int[] scatterModeTimes;
    public int[] chaseModeTimes;
    public int maxIterationModes;
    public float superPelletMaxDuration;
    public float pacManSpeed;
    public int pacManStartingLives;
    public float fruitLifeTime;
}

[System.Serializable]
public class GhostConfigs
{
    public string ghostName;
    public float timeToRelease;
    public float ghostMoveSpeed;
    public float ghostConsumedSpeed;
}

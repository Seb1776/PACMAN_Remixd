using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class MainMenu : MonoBehaviour
{
    public string sceneToLoad;
    public TMP_Text timeTrialModeTimer;
    public AudioClip selectionSFX;
    public Animator menuAnimator;
    public Animator fadePanel;
    [Header ("General Gamemode")]
    public Maze[] allLevels;
    public DifficultySetting[] allDifficulties;
    public TMP_Text gamemodeName;
    public Image levelPreview, _levelPreview, __levelPreview;
    public TMP_Text levelName, _levelName, __levelName;
    public TMP_Text difficultyName, _difficultyName, __difficultyName;
    public TMP_FontAsset ultraNMaterial;
    public GameObject classicPlay, timeTrialPlay, PVP2PPlay;
    [Header ("PVP2P Specific")]
    public Image pac1;
    public Image pac2;
    public Color currentPlayer1Color;
    public Color currentPlayer2Color;
    public List<ColoredButtons> customizeP1 = new List<ColoredButtons>();
    public List<ColoredButtons> customizeP2 = new List<ColoredButtons>();
    public GameSetter gameSetter;

    [SerializeField] Button p1LastPressed;
    [SerializeField] Button p2LastPressed;
    TMP_FontAsset originalFA;
    int currentLevelIndex;
    GameManager.GameMode currentGameMode;
    int maxLevelIndex;
    int currentDifficultyIndex;
    int maxDifficultyIndex;
    AudioSource source;
    float timeSpent = 12f;

    void Awake()
    {
        Screen.SetResolution(1280, 720, true);
        source = GetComponent<AudioSource>();
    }

    void Start()
    {
        maxLevelIndex = allLevels.Length - 1;
        maxDifficultyIndex = allDifficulties.Length - 1;
        UpdateLevelShow();
        UpdateDifficulty();
    }

    void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
            ChangeScene(sceneToLoad);
        
        TimeTrialTimerAnimation();
    }

    void TimeTrialTimerAnimation()
    {
        if (timeSpent < 2f)
            timeSpent = 12f;
        
        timeSpent -= Time.deltaTime;

        timeTrialModeTimer.text = TimeSpan.FromSeconds(timeSpent).Minutes.ToString("D2") + ":" + TimeSpan.FromSeconds(timeSpent).Seconds.ToString("D2") + ":" +
                        TimeSpan.FromSeconds(timeSpent).Milliseconds.ToString();
    }

    public void ChangeMenuSection(string section)
    {
        switch (section)
        {
            case "MainMenu":
                menuAnimator.SetTrigger("gamemodes_menu");
            break;

            case "Gamemodes":
                menuAnimator.SetTrigger("menu_gamemodes");
            break;

            case "ToPlay":
                menuAnimator.SetTrigger("gamemodes_toplay");
            break;

            case "ToPlayGamemodes":
                menuAnimator.SetTrigger("toplay_gamemodes");
                currentLevelIndex = 0;
                UpdateLevelShow();
                UpdateDifficulty();
            break;

            case "Play":

            break;
        }

        source.PlayOneShot(selectionSFX);
    }

    public void LevelSelect(bool _or)
    {
        if (_or)
        {
            currentLevelIndex++;

            if (currentLevelIndex > maxLevelIndex)
                currentLevelIndex = 0;
        }

        else
        {
            currentLevelIndex--;

            if (currentLevelIndex < 0)
                currentLevelIndex = maxLevelIndex;
        }

        UpdateLevelShow();
        source.PlayOneShot(selectionSFX);
    }

    public void DifficultySelect(bool _or)
    {
        if (_or)
        {
            currentDifficultyIndex++;

            if (currentDifficultyIndex > maxDifficultyIndex)
                currentDifficultyIndex = 0;
        }

        else
        {
            currentDifficultyIndex--;

            if (currentDifficultyIndex < 0)
                currentDifficultyIndex = maxDifficultyIndex;
        }

        UpdateDifficulty();
        source.PlayOneShot(selectionSFX);
    }

    public void SetPlayer1Color(Button btn)
    {
        pac1.color = currentPlayer1Color =  btn.GetComponent<Image>().color;

        for (int i = 0; i < customizeP1.Count; i++)
        {
            if (customizeP1[i].btn == btn)
            {
                btn.interactable = false;
                customizeP2[i].btn.interactable = false;

                if (p1LastPressed == null)
                    p1LastPressed = btn;
                
                else
                {
                    p1LastPressed.interactable = true;

                    int relativeIndex = -1;

                    for (int j = 0; j < customizeP1.Count; j++)
                    {
                        if (customizeP1[j].btn == p1LastPressed)
                        {
                            relativeIndex = j;
                            break;
                        }
                    }

                    customizeP2[relativeIndex].btn.interactable = true;
                    p1LastPressed = btn;
                }
            }
        }
    }

    public void SetPlayer2Color(Button btn)
    {
        pac2.color = currentPlayer2Color =  btn.GetComponent<Image>().color;

        for (int i = 0; i < customizeP2.Count; i++)
        {
            if (customizeP2[i].btn == btn)
            {
                btn.interactable = false;
                customizeP1[i].btn.interactable = false;

                if (p2LastPressed == null)
                    p2LastPressed = btn;
                
                else
                {
                    int relativeIndex = -1;
                    p2LastPressed.interactable = true;

                    for (int j = 0; j < customizeP2.Count; j++)
                    {
                        if (customizeP2[j].btn == p2LastPressed)
                        {
                            relativeIndex = j;
                            break;
                        }
                    }

                    customizeP1[relativeIndex].btn.interactable = true;
                    p2LastPressed = btn;
                }
            }
        }
    }

    void UpdateLevelShow()
    {
        levelName.text = allLevels[currentLevelIndex].mazeName;
        _levelName.text = allLevels[currentLevelIndex].mazeName;
        __levelName.text = allLevels[currentLevelIndex].mazeName;
        levelPreview.sprite = allLevels[currentLevelIndex].levelPreview;
        _levelPreview.sprite = allLevels[currentLevelIndex].levelPreview;
        __levelPreview.sprite = allLevels[currentLevelIndex].levelPreview;
    }

    void UpdateDifficulty()
    {
        difficultyName.text = allDifficulties[currentDifficultyIndex].difficultyName;
        _difficultyName.text = allDifficulties[currentDifficultyIndex].difficultyName;
        __difficultyName.text = allDifficulties[currentDifficultyIndex].difficultyName;
    }

    public void SendInfoToSetter()
    {
        fadePanel.SetTrigger("fade");

        gameSetter.selectedDifficulty = allDifficulties[currentDifficultyIndex];
        gameSetter.selectedMaze = allLevels[currentLevelIndex].mazeName;
        gameSetter.p1Color = currentPlayer1Color;
        gameSetter.p2Color = currentPlayer2Color;
        gameSetter.selectedMode = currentGameMode;

        ChangeScene(gameSetter.selectedMaze);
    }

    public void SetSpecificScreen(string _gamemodeName)
    {
        gamemodeName.text = _gamemodeName;

        if (_gamemodeName == "Classic")
        {
            classicPlay.SetActive(true);
            timeTrialPlay.SetActive(false);
            PVP2PPlay.SetActive(false);
            currentGameMode = GameManager.GameMode.Classic;
        }

        else if (_gamemodeName == "Time Trial")
        {
            classicPlay.SetActive(false);
            timeTrialPlay.SetActive(true);
            PVP2PPlay.SetActive(false);
            currentGameMode = GameManager.GameMode.TimeTrial;
        }

        else if (_gamemodeName == "Pacman VS Pacman")
        {
            classicPlay.SetActive(false);
            timeTrialPlay.SetActive(false);
            PVP2PPlay.SetActive(true);
            currentGameMode = GameManager.GameMode.PVP2P;
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ChangeScene(string sceneName)
    {
        StartCoroutine(LoadScene(sceneName));
    }

    IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        while (op.isDone)
        {
            yield return null;
        }
    }
}


[System.Serializable]
public class ColoredButtons
{
    public Button btn;
}

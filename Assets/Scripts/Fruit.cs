using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fruit : MonoBehaviour
{
    public int scoreValue;
    public float lifeTime;
    public AudioClip collectedFruit;

    GameManager manager;

    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    public void CollectedFruit()
    {   
        if (manager.currentGamemode != GameManager.GameMode.TimeTrial)
        {
            manager.fruits[manager.fruitIndex].GetComponent<Animator>().ResetTrigger("lock");
            manager.fruits[manager.fruitIndex].GetComponent<Animator>().SetTrigger("collected");
            manager.classicHUDWin[manager.currentActIndex].fruitActs[manager.fruitIndex].grayFruit.SetActive(false);
            manager.classicHUDWin[manager.currentActIndex].fruitActs[manager.fruitIndex].colorFruit.SetActive(true);
            manager.classicHUDWin[manager.currentActIndex].collectedFruits++;
            manager.fruitIndex++;
            manager.pelletFruitCounter = 0;

            if (manager.fruitIndex > manager.fruits.Length - 1)
                manager.ateAllFruits = true;
        }

        else if (manager.currentGamemode == GameManager.GameMode.TimeTrial)
        {
            manager.timeTrialEatenFruits++;
            manager.StopTimer();
        }

        manager.createdFruit = null;
        manager.score += scoreValue;
        Destroy(this.gameObject);
    }

    public void SpecificPlayerCollectedFruit(Player player)
    {
        if (player == manager.pacMan)
        {
            manager.playerOneFruits[manager.fruitIndex].GetComponent<Animator>().ResetTrigger("lock");
            manager.playerOneFruits[manager.fruitIndex].GetComponent<Animator>().SetTrigger("collected");
            manager.score += scoreValue;
        }

        else if (player == manager.pacMan2)
        {
            manager.playerTwoFruits[manager.fruitIndex].GetComponent<Animator>().ResetTrigger("lock");
            manager.playerTwoFruits[manager.fruitIndex].GetComponent<Animator>().SetTrigger("collected");
            manager.p2Score += scoreValue;
        }

        manager.fruitIndex++;
        manager.pelletFruitCounter = 0;

        if (manager.fruitIndex > manager.playerOneFruits.Length - 1)
            manager.ateAllFruits = true;
        
        manager.createdFruit = null;
        Destroy(this.gameObject);
    }

    public void DestroyFruit()
    {
        StartCoroutine(WaitToDestroy());
    }

    IEnumerator WaitToDestroy()
    {
        yield return new WaitForSeconds(lifeTime);
        manager.fruitIndex++;
        manager.pelletFruitCounter = 0;
        Destroy(this.gameObject);
    }

    public void InstaDestroyFruit()
    {
        Destroy(this.gameObject);
        manager.fruitIndex++;
        manager.pelletFruitCounter = 0;
    }
}

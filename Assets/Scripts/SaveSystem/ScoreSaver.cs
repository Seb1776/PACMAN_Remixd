using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreSaver
{
    public List<ScoreData> scoreDatas = new List<ScoreData>();
}

public class ScoreData
{
    public string levelName;
    //Classic
    public int bestClassicScore;
    public float bestAct1Time, bestAct2Time, bestAct3Time;
    public float bestGlobalTime;
    //Time Trial
    public float bestSurvivedTime;
    public int bestTimeTrialScore;
}

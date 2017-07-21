using NCMBRest;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(NCMBRestController))]
public class LeaderboardManager : MonoBehaviour
{
    private NCMBRestController ncmbRestController;

    private static readonly string OBJECT_ID = "ObjectId";
    private static readonly string HIGH_SCORE = "HighScore";
    private static readonly string DATASTORE_CLASSNAME = "Leaderboard";

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        ncmbRestController = GetComponent<NCMBRestController>();
    }

    public IEnumerator SendScore(string playerName, int score, bool isAllowDuplicatedScore)
    {
        NCMBDataStoreParamSet paramSet = new NCMBDataStoreParamSet(new Score(playerName, score));

        //与えられたスコアが自己ハイスコアかどうか確認してからポスト または重複OKかどうか//
        if (!PlayerPrefs.HasKey(OBJECT_ID) || isAllowDuplicatedScore)
        {
            Debug.Log("Post Data " + playerName);

            IEnumerator coroutine = ncmbRestController.Call(NCMBRestController.RequestType.POST, "classes/" + DATASTORE_CLASSNAME, paramSet);

            yield return StartCoroutine(coroutine);

            //取得したjsonをNCMBDataStoreParamSetとして展開//
            paramSet = JsonUtility.FromJson<NCMBDataStoreParamSet>((string)coroutine.Current);

            PlayerPrefs.SetString(OBJECT_ID, paramSet.objectId);
            PlayerPrefs.SetInt(HIGH_SCORE, score);
        }
        else
        {
            if (score > PlayerPrefs.GetInt(HIGH_SCORE))
            {
                string objectId = PlayerPrefs.GetString(OBJECT_ID);
                Debug.Log("This PC's ID " + objectId + "Put Data" + playerName);

                IEnumerator coroutine = ncmbRestController.Call(NCMBRestController.RequestType.PUT, "classes/" + DATASTORE_CLASSNAME + "/" + objectId, paramSet);

                yield return StartCoroutine(coroutine);

                PlayerPrefs.SetInt(HIGH_SCORE, score);
            }
            else
            {
                Debug.Log("Score doesn't updated");
            }
        }
    }
    
    public IEnumerator GetScoreList(UnityAction<Scores> callback)
    {
        Debug.Log("Get Data");
        NCMBDataStoreParamSet paramSet = new NCMBDataStoreParamSet();
        paramSet.Limit = 10;
        paramSet.SortColumn = "-score";

        IEnumerator coroutine = ncmbRestController.Call(NCMBRestController.RequestType.GET, "classes/" + DATASTORE_CLASSNAME, paramSet);

        yield return StartCoroutine(coroutine);

        string jsonStr = (string)coroutine.Current;

        //取得したjsonをScoresとして展開//
        Scores scores = JsonUtility.FromJson<Scores>(jsonStr);

        if (scores.results.Count == 0)
        {
            Debug.Log("no data");
        }

        callback(scores);
    }

    public IEnumerator GetScoreList(UnityAction<string> callback)
    {
        yield return GetScoreList((scores) =>
        {
            string str = string.Empty;

            int i = 1;

            foreach (Score s in scores.results)
            {
                str += i + ": " + s.playerName + ": " + s.score.ToString() + "\n";
                i++;
            }

            callback(str);
        });
    }

    [Serializable]
    public class Scores
    {
        public List<Score> results;
    }

    [Serializable]
    public class Score
    {
        public Score(string playerName, int score)
        {
            this.playerName = playerName;
            this.score = score;
        }

        public string playerName;
        public int score;
    }
}
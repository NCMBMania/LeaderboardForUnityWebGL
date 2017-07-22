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
    private static readonly string DATASTORE_CLASSNAME = "Leaderboard"; //スコアを保存するデータストア名//

    public static LeaderboardManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)

        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        ncmbRestController = GetComponent<NCMBRestController>();
    }

    public IEnumerator SendScore(string playerName, int score, bool isAllowDuplicatedScore = false)
    {
        if (isAllowDuplicatedScore == false)
        {
            //過去のスコアがあるか//
            if (PlayerPrefs.HasKey(OBJECT_ID))
            {
                //そのスコアはハイスコアか//
                if (score > PlayerPrefs.GetInt(HIGH_SCORE))
                {
                    //レコードの更新//
                    yield return PutScore(playerName, score, PlayerPrefs.GetString(OBJECT_ID));
                    //ローカルのハイスコアを更新//
                    PlayerPrefs.SetInt(HIGH_SCORE, score);
                    yield break;
                }
                else
                {
                    Debug.Log("Score doesn't updated");
                    yield break;
                }
            }
        }

        //レコードの新規作成//
        IEnumerator postScoreCoroutine = PostScore(playerName, score);

        yield return postScoreCoroutine;

        string objectId = (string)postScoreCoroutine.Current;

        PlayerPrefs.SetString(OBJECT_ID, objectId);//ObjectIdを保存//
        PlayerPrefs.SetInt(HIGH_SCORE, score);//ローカルのハイスコアを保存//
    }

    private IEnumerator PostScore(string playerName, int score)
    {
        Debug.Log("Post Score " + playerName);

        ScoreData scoreData = new ScoreData(playerName, score);
        NCMBDataStoreParamSet paramSet = new NCMBDataStoreParamSet(scoreData);

        IEnumerator coroutine = ncmbRestController.Call(NCMBRestController.RequestType.POST, "classes/" + DATASTORE_CLASSNAME, paramSet);

        yield return StartCoroutine(coroutine);

        JsonUtility.FromJsonOverwrite((string)coroutine.Current, paramSet);

        yield return paramSet.objectId;
    }

    private IEnumerator PutScore(string playerName, int score, string objectId)
    {
        Debug.Log("Put Score " + playerName + "This PC's ID " + objectId);

        ScoreData scoreData = new ScoreData(playerName, score);
        NCMBDataStoreParamSet paramSet = new NCMBDataStoreParamSet(scoreData);

        IEnumerator coroutine = ncmbRestController.Call(NCMBRestController.RequestType.PUT, "classes/" + DATASTORE_CLASSNAME + "/" + objectId, paramSet);

        yield return StartCoroutine(coroutine);

        JsonUtility.FromJsonOverwrite((string)coroutine.Current, paramSet);

        yield return paramSet.objectId;
    }

    public IEnumerator GetScoreList(int num, UnityAction<ScoreDatas> callback)
    {
        Debug.Log("Get Data");
        NCMBDataStoreParamSet paramSet = new NCMBDataStoreParamSet();
        paramSet.Limit = num;
        paramSet.SortColumn = "-score";

        IEnumerator coroutine = ncmbRestController.Call(NCMBRestController.RequestType.GET, "classes/" + DATASTORE_CLASSNAME, paramSet);

        yield return StartCoroutine(coroutine);

        string jsonStr = (string)coroutine.Current;

        //取得したjsonをScoreDatasとして展開//
        ScoreDatas scores = JsonUtility.FromJson<ScoreDatas>(jsonStr);

        if (scores.results.Count == 0)
        {
            Debug.Log("no data");
        }

        callback(scores);
    }

    public IEnumerator GetScoreListByStr(int num, UnityAction<string> callback)
    {
        yield return GetScoreList(num, (scores) =>
        {
            string str = string.Empty;

            int i = 1;

            foreach (ScoreData s in scores.results)
            {
                str += i + ": " + s.playerName + ": " + s.score.ToString() + "\n";
                i++;
            }

            callback(str);
        });
    }

    [Serializable]
    public class ScoreDatas
    {
        public List<ScoreData> results;
    }

    [Serializable]
    public class ScoreData
    {
        public ScoreData(string playerName, int score)
        {
            this.playerName = playerName;
            this.score = score;
        }

        public string playerName;
        public int score;
    }
}
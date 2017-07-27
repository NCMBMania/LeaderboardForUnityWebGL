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

    private static readonly string PLAYERNAME = "PlayerName";
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

    public IEnumerator SendScore(string playerName, int score, bool isAllowDuplication = false)
    {
        //ユーザーごとのスコアの重複を許すか//
        if (isAllowDuplication == false)
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
                    PlayerPrefs.SetString(PLAYERNAME, playerName);
                    yield break;
                }
                else
                {
                    Debug.Log("ハイスコアが更新されていないため、スコアを送信しませんでした。");
                    yield break;
                }
            }
        }

        yield return SendScoreUncheck(playerName, score);
    }

    private IEnumerator SendScoreUncheck(string playerName, int score)
    {
        //レコードの新規作成//
        IEnumerator postScoreCoroutine = PostScore(playerName, score);

        yield return postScoreCoroutine;

        string objectId = (string)postScoreCoroutine.Current;

        PlayerPrefs.SetString(OBJECT_ID, objectId);//ObjectIdを保存//
        PlayerPrefs.SetInt(HIGH_SCORE, score);//ローカルのハイスコアを保存//
        PlayerPrefs.SetString(PLAYERNAME, playerName);//プレイヤーネームを保存 名前を変えたときのチェック用//
    }

    private IEnumerator PostScore(string playerName, int score)
    {
        Debug.Log(playerName + "のスコア" + score + "を新規投稿します。");

        ScoreData scoreData = new ScoreData(playerName, score);
        NCMBDataStoreParamSet paramSet = new NCMBDataStoreParamSet(scoreData);

        IEnumerator coroutine = ncmbRestController.Call(NCMBRestController.RequestType.POST, "classes/" + DATASTORE_CLASSNAME, paramSet);

        yield return StartCoroutine(coroutine);

        JsonUtility.FromJsonOverwrite((string)coroutine.Current, paramSet);

        yield return paramSet.objectId;
    }

    private IEnumerator PutScore(string playerName, int score, string objectId)
    {
        string formerPlayerName = PlayerPrefs.GetString(PLAYERNAME);

        if(formerPlayerName != playerName)
        {
            Debug.Log("プレイヤー名が " + formerPlayerName + " から " + playerName + " に変更されました");
            PlayerPrefs.SetString(PLAYERNAME, playerName);
        }

        Debug.Log(playerName+"のスコア"+score + "を更新します。レコードのID：" + objectId);

        ScoreData scoreData = new ScoreData(playerName, score);
        NCMBDataStoreParamSet paramSet = new NCMBDataStoreParamSet(scoreData);

        IEnumerator coroutine = ncmbRestController.Call(
            NCMBRestController.RequestType.PUT, "classes/" + DATASTORE_CLASSNAME + "/" + objectId, paramSet, 
            (erroCode) => 
            {
                if(erroCode == 404)
                {
                    Debug.Log("レコードID：" + objectId +"が見つからなかったため、新規レコードを作成します");
                    StartCoroutine(SendScoreUncheck(playerName, score));
                }
            }

            );

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

    public void ClearLocalData()
    {
        PlayerPrefs.DeleteKey(PLAYERNAME);
        PlayerPrefs.DeleteKey(OBJECT_ID);
        PlayerPrefs.DeleteKey(HIGH_SCORE);
        Debug.Log("ローカルのハイスコアとObjectIdが削除されました");
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
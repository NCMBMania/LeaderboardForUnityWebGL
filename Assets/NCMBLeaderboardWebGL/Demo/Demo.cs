﻿using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    public int score;

    public InputField playerNameInputField;
    public Text scoreText;

    public Image leaderboard;
    public Text leaderBoardText;
    public Button hideButton;

    private void Awake()
    {
        OnButtonHideLeaderBoard();
    }

    public void OnButtonCountUp()
    {
        score++;
        scoreText.text = score.ToString();
    }

    public void OnButtonScoreSend()
    {
        string playerName = string.IsNullOrEmpty(playerNameInputField.text) ? "No Name" : playerNameInputField.text;

        //一人でテストするとき用に１つのPCからスコアを複数回追加できる重複ＯＫオプションをtrueにしてます。本番用はfalse推奨。//
        StartCoroutine(LeaderboardManager.Instance.SendScore(playerName, score, true));
    }

    public void OnButtonShowLeaderBoard()
    {
        leaderBoardText.text = "Loading...";
        leaderboard.enabled = true;

        //上位10位まで取得//
        StartCoroutine(LeaderboardManager.Instance.GetScoreListByStr(10, (scoresText) =>
        {
            leaderBoardText.text = scoresText;
        }));
    }

    public void OnButtonHideLeaderBoard()
    {
        leaderboard.enabled = false;
        leaderBoardText.text = string.Empty;
    }
}
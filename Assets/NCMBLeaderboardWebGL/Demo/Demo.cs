using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    public LeaderboardManager leaderboardManager;
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
        StartCoroutine(leaderboardManager.SendScore(playerName, score, true));
    }

    public void OnButtonShowLeaderBoard()
    {
        leaderBoardText.text = "Loading...";
        leaderboard.enabled = true;

        StartCoroutine(leaderboardManager.GetScoreList((string scoreList) =>
        {
            leaderBoardText.text = scoreList;
        }));
    }

    public void OnButtonHideLeaderBoard()
    {
        leaderboard.enabled = false;
        leaderBoardText.text = string.Empty;
    }
}
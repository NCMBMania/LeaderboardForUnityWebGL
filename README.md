# Leaderboard For Unity WebGL

## これは何？

Unity WebGLで簡単に使えるランキング（リーダーボード）プラグインです。
主にUnity WebGLで開発されたゲームの公開サービス、[unityroom](https://unityroom.com/)のために用意しました。
PC/Mac/iOS/Androidでもそのままご利用いただけます。
ライセンスはMITです。
スコアを保存するバックエンドにはニフティクラウド mobile backend(NCMB)を使用します。

http://mb.cloud.nifty.com/

本ライブラリは[anzfactory](https://github.com/anzfactory)さんの「Yoshinani」ライブラリをベースとしています。
本人に許可をいただいた上で内容を発展させ、ゲーム中におけるプレイヤーごとのログインが不要になっています。
https://github.com/anzfactory/Yoshinani

（REST通信のための文字列生成処理などは、ほとんど同じです。）

## デモの試し方

ブラウザで試せるデモは[ここ](https://unityroom.com/games/leaderboarddemo)

Unity Editor上で試したり、自分のゲームに導入する場合はNCMBのアカウント取得が必要です。

1. NCMBのアカウントを作り、「アプリ」を作成してAPIキー(Aplication KeyとClient Key)を入手する。
2. releseのLeaderboardForWebGL.unitypackageをプロジェクトにインポートする
3. Demo.sceneを開く
4. LeaderboardManagerオブジェクトのインスペクタについている「NCMB Rest Controller」にAplication KeyとClient Keyを設定する
5. 実行、Count Upで数字が増加、Send Scoreでスコアを送信、Show Leaderboardでランキングを取得・表示。

サンプルのランキング表示画面はCanvasを使ったシンプルなものですが、このままゲームへパクっても問題ありません。

![デモ](Images/Demo.png)


## ゲームへの組み込み方

### プレハブの配置
まず、Assets\NCMBLeaderboardWebGL\Prefab\LeaderboardManager をシーン上に配置します。
LeaderboardManagerはシングルトンクラスですので、他のスクリプトからはLeaderboardManager.Instanceでアクセスできます。

### スコアを送信

LeaderboardManager.csの次の関数をコルーチンとして呼びます。

```csharp

StartCoroutine(LeaderboardManager.Instance.SendScore(playerName, score, false));

```
プレイヤーの名前とスコアを与えてコルーチンを実行します。

引数の「isAllowDuplicatedScore」は、プレイヤー1人ごとのスコアの保持を1つにするか、複数にするかのフラグです。
自分でテストする際はレコードが1個しか登録できないと作りづらいのでtrueにしておき、本番ではfalseにするとよいと思います。
（詳しくは下記の「仕様」をご覧下さい）

NCMBの管理画面では、「データストア」にLeaderboardという名前のクラスができ、ここにデータが溜まっていきます。

![管理画面「データストア」](Images/Console.png)

### リーダーボードを表示

NCMBに保存したスコアの一覧を取得する場合は、LeaderboardManager.GetScoreListByStr()を使います。
引数に上位いくつまでのスコアを取得したいかと、処理が終わった後のコールバックを渡します。
複数行のテキストに成形された状態で渡されるので、そのままUI.Text.textに渡せばすぐ表示できます。

```csharp

StartCoroutine(LeaderboardManager.Instance.GetScoreListByStr(10, (scores) =>
{
    leaderBoardText.text = scores;
}));

```
ランキングのリストをテキストの塊ではなく、順位ごとにサイズを変えたり、何か効果をつけたい場合は結果をScores()クラスで受け取ることもできます。
Scoresの中身はScoreクラスのリストで、ScoreクラスはフィールドplayerName, scoreを持っています。

```csharp

        StartCoroutine(LeaderboardManager.Instance.GetScoreList(10, (LeaderboardManager.ScoreDatas scoreDatas) =>
        {
            int i = 0;
            foreach(LeaderboardManager.ScoreData scoreData in scoreDatas.results)
            {
                leadarboardPlayerNameText[i].text = scoreData.playerName;
                leadarboardScoreText[i].text = scoreData.score;
                i++;
            }
        }));
```

などのように、任意のUIパーツに情報を流し込むことができます。


## 仕様
このリーダーボードは、1人のプレイヤーは記録を1つしか持てないようになっています。
99点と98点を出した場合でも、リーダーボード上は99点の方しか表示されません。

内部的には、NCMBへスコアを保存したタイミングで、PCごとにIDが割り振っています。
これは、NCMB側から振り出されたレコードIDを利用したもので、PlayePrefsを使って、ローカルに「ObjectId」として保存されます。
つぎにデータを保存しようとした際に、PlayerPrefsにObjectIDが保存されている場合は、新規にレコードを作成せず、同じOjbectIDのレコードを更新します。

ローカルには同時に自己ハイスコアも記録しています。
スコアを更新しようとした際、この自己スコアと比較して大きかった場合のみ送信処理を行っています。

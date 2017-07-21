# LeaderboardForUnityWebGL

## これは何？

Unityで開発したゲームをWebGL出力したときに使えるランキング（リーダーボード）機能を追加するものです。
リーダーボードデータを保存するバックエンドにニフティクラウド mobile backend(NCMB)を使用しています。

http://mb.cloud.nifty.com/

[anzfactory](https://github.com/anzfactory)さんのYoshinaniライブラリを発展させて開発しました。プレイヤーごとのログインが不要になっています。

https://github.com/anzfactory/Yoshinani

REST通信のための文字列成形などはほとんどいっしょです。

## 仕様
スコアを保存すると、PCごとにIDが割り振られます。
IDを持っている場合は、同IDのデータの内容を更新します。すなわち、自分のハイスコアを超えない限りはスコアが更新されません。
PlayePrefsで、ObjectIdとHighScoreとしてローカルに保存されます。

## デモの使い方

1. NCMBのアカウントを作り、「アプリ」を作成してAPIキー(Aplication KeyとClient Key)を入手する。
2. releseのLeaderboardForWebGL.unitypackageをプロジェクトにインポートする
3. Demo.sceneを開く
4. LeaderboardManagerオブジェクトのインスペクタについている「NCMB Rest Controller」にAplication KeyとClient Keyを設定する
5. 実行、Count Upで数字が増加、Send Scoreでスコアを送信、Show Leaderboardでランキングを取得・表示。

NCMBの管理画面では、「データストア」にLeaderboardという名前のクラスができており、ここにデータが溜まっていきます。

## 組み込み方

### プレハブの配置
まず、Assets\NCMBLeaderboardWebGL\Prefab\LeaderboardManager をシーン上に配置します。

### スコアを送信

通信処理はコルーチンで処理します。LeaderboardManager.csの次の関数を呼びます。

```csharp

StartCoroutine(leaderboardManager.SendScore(playerName, score, false));

```
プレイヤーの名前、スコアを与えます。
引数のiSisAllowDuplicatedScoreは、スコアが一人（一つのPC）から何回も投稿できるオプションです。
自分でテストする際はtrueにしておき、本番ではfalseにするとよいです。

### リーダーボードを表示

コールバックに取得結果が戻ってきますので、ゲームのUI表示スクリプトに渡してあげてください。

```csharp

StartCoroutine(leaderboardManager.GetScoreList(UnityAction<Scores> callback));

```

リーダーボードの形に成形された複数行のstringで結果を取得することもできます。
デモではこれを使って実装しています。

```csharp

StartCoroutine(leaderboardManager.GetScoreList(UnityAction<string> callback));

```

# おみせやさん物語（Unity 版）

Web 版（`shop/index.html`）と同じルールのお店経営ゲームを、Unity の 3D で動かすためのスクリプトです。
いまは **仮の見た目**（箱や円柱で作った店内とお客さん）で、ゲームを最初から最後まで遊べる状態です。
リアルな 3D モデルやきれいな UI には、このあと差し替えていきます。

## 動かし方

1. **Unity Hub** をインストールして、**Unity 6（6000.0 LTS 以降）** を入れる
   - インストール時のモジュールは、とりあえず何も追加しなくて OK（スマホに出すときに Android / iOS を追加）
2. Unity Hub で **New project** → テンプレート **「Universal 3D」**（URP）を選んで、新しいプロジェクトを作る
3. このリポジトリの `unity/Assets/OmiseYasan` フォルダを、作ったプロジェクトの `Assets` フォルダの中にコピーする
4. Unity に戻ると自動でコンパイルされる。最初から開いている `SampleScene` のまま **再生ボタン（▶）** を押す
5. 店内とタイトル画面が出たら成功

シーンに何も置かなくても、再生すると自動でお店・カメラ・UI が組み立てられます
（`GameController.cs` の `[RuntimeInitializeOnLoadMethod]`）。
自動で組み立てたくないシーンを作るときは、`GameController.AutoBoot = false;` にしてください。

### 画面の向き

- **横長**（PC）: 左に 3D の店内、右に操作パネル
- **縦長**（スマホ）: 上に 3D の店内、下に操作パネル

Game ビューの解像度を「1920x1080」や「1080x1920 Portrait」に切り替えると、両方を確かめられます。

### うまくいかないとき

| こまったこと | なおし方 |
|---|---|
| 日本語が「□」になる | `Assets` に日本語フォント（例: [Noto Sans JP](https://fonts.google.com/noto/specimen/Noto+Sans+JP)）を入れて、`GameUI.MakeStyles()` の各スタイルの `font` に設定する |
| 真っピンクの物体がある | プロジェクトが URP ではない、または描画設定が壊れている。「Universal 3D」テンプレートで作り直す（Built-in でも動くはず） |
| セーブを消したい | メニュー **Edit → Clear All PlayerPrefs** |

## フォルダの中身

```
Assets/OmiseYasan/Scripts/
  Core/                 ゲームのルール（Unity に依存しない C#）
    GameData.cs         お店・場所・お客さん13種・商品のデータ
    GameState.cs        セーブされる状態
    Game.cs             お客さんを作る／判定する／決算／お店を育てる
  Runtime/              Unity 用
    GameController.cs   全体のまとめ役（画面の流れ・セーブ）
    ShopView.cs         店内の 3D と、時間で変わる明かり（昼→夕方→夜）
    PersonFactory.cs    仮の人間（大人・子ども・お年寄り・ベビーカー）
    CustomerCrowd.cs    お客さんの動き（入店→ウロウロ→喜ぶ／首をふって帰る）
    GameUI.cs           仮の UI（IMGUI）と、頭の上のふきだし
Tools/Sim/              Unity なしでルールを動かすシミュレーター
```

**ルール（Core）と見た目（Runtime）を分けてあります。**
3D モデルや UI をどれだけ作り替えても、ゲームのルールは `Game` クラスのまま使えます。

## バランスの確認（Unity なしで）

[.NET SDK 8](https://dotnet.microsoft.com/download) があれば、ルールだけを何百回も自動で遊ばせて、何日でクリアできるかを確かめられます。

```
cd unity/Tools/Sim
dotnet run -- 0.7     # 0.7 = 70% の確率でお客さんの欲しいものを当てるプレイヤー
```

いまの設定だと、読みがうまい人（0.7）は約16〜24日、苦手な人（0.4）は約21〜47日でチェーン店になります。

## リアルにしていく順番（おすすめ）

1. **お客さんを人間のモデルにする**
   - [Quaternius](https://quaternius.com/)（無料・CC0）や Unity Asset Store の人間モデル + [Mixamo](https://www.mixamo.com/) の歩く・待つアニメーション
   - `PersonFactory.Build()` を「Prefab を Instantiate して `PersonRig` を付ける」形に変え、`PersonAgent` の手足を動かす部分を Animator に置き換える
2. **店内を本物の家具にする**
   - [Kenney](https://kenney.nl/)（無料・CC0）の家具や食べ物、Asset Store のお店素材
   - `ShopView` の `BuildRamen()` / `BuildSalon()` / `BuildCake()` をそれぞれ Prefab の配置に置き換える
3. **光をリアルにする**
   - URP の Volume で Bloom・Color Adjustments・Ambient Occlusion を追加
   - 明かりをベイクして、影をやわらかくする
4. **UI を作り直す**
   - UI Toolkit か uGUI + TextMeshPro で、Web 版と同じ「のれん」「値札」のデザインにする
5. **スマホに出す**
   - Build Profiles で Android / iOS を選んでビルド

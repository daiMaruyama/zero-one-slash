# ZeroOneSlash 🎯

> ダーツ × エイム × 暗算 — 制限時間内にターゲットスコアを"ゼロ"にせよ！

[![Unity](https://img.shields.io/badge/Unity-2022.3.61f1-black?logo=unity)](https://unity.com/)
[![Award](https://img.shields.io/badge/学内審査会-優秀賞-gold)]()

🎮 **[UnityRoomで遊ぶ](https://unityroom.com/games/zerooneslash)**

---

## 🎮 ゲーム概要

ランダムに出題されるターゲットスコア（1〜180）を、ダーツ3投以内にピッタリ0にするゲーム。
制限時間60秒の中で何問クリアできるかを競う。

エイム力（正確に狙う）と暗算力（どこを狙えば0になるか）の両方が問われる、シンプルだけど奥深いハイスコアチャレンジ。

## 🕹️ 操作方法

| 操作 | アクション |
|------|----------|
| マウス移動 | エイム（照準移動） |
| クリック | ダーツを投げる |

## 📐 ルールとスコアリング

### 基本ルール
- ターゲットスコアがランダムに出題される（1〜180、3投で到達可能な数のみ）
- **3投以内**にスコアをピッタリ0にすればクリア
- 制限時間 **60秒**

### 判定
| 結果 | 条件 | 獲得スコア |
|------|------|-----------|
| **GREAT WIN!!** | Double / Triple / Bull でフィニッシュ | +500 |
| **WIN!!** | Single でフィニッシュ | +100 |
| **BUST** | 残りスコアがマイナスになった | +0 |
| **NO OUT** | 3投使い切って残りが0にならなかった | +0 |
| **MISS** | ボード外に投げた | +0（投数消費） |

### コンボシステム
- ヒットするたびにコンボが上昇
- コンボ数に応じて制限時間が少し回復（寿司打風）
- GREAT エリア3回ヒットでボーナス回復
- 高コンボ時は画面揺れ＆Bloom演出で盛り上がる
- BUST / MISS / NO OUT でコンボリセット

## ✨ 特徴

- **ネオンレッドテーマ** — サイバーパンク風のUI演出
- **BGMテンポ演出** — 残り時間が少なくなるとBGMのピッチが上がる
- **ヒットストップ** — Double/Triple/Bull ヒット時にカメラズーム＆シェイク
- **オンラインランキング** — Unity Gaming Services (UGS) 連携
- **ニューレコード演出** — ランキング入り時に名前入力＆特別演出
- **遊び方パネル** — 4ページスライド形式のチュートリアル

## 🛠️ 技術スタック

| カテゴリ | 技術 |
|---------|------|
| **Engine** | Unity 2022.3.61f1 |
| **Language** | C# |
| **Ranking** | Unity Gaming Services (UGS) Leaderboard |
| **Animation** | DOTween |
| **Build Target** | WebGL |

### 設計パターン
- **Builder パターン** — UI パネルの動的生成（HowToPlay, Ranking, NewRecord, Result）
- **Singleton** — AudioManager, GameEffectsManager, BloomManager 等
- **コンポーネント分離** — Core / UI / Effects / Audio / Camera / Rendering / Ranking

## 📁 プロジェクト構成

```
Assets/Scripts/
├── Core/            # GameManager, Card, CheckoutAdvisor, DartsBoard
├── UI/              # TitleController, ResultPanel, HowToPlay, NeonButton系
├── Effects/         # BloomManager, HitStop, HitPopup, NumberPop
├── Audio/           # AudioManager, VolumeController
├── Camera/          # CameraController, CameraShake
├── Rendering/       # GlobalVolume, Shader制御
└── RankingScripts/  # RankingManager, NewRecordPanel, RankingPanel
```

## 🏆 受賞

**学内審査会 優秀賞**（2026.02.17）

## 👤 作成者

**Maruyama Daisuke**
- GitHub: [@daiMaruyama](https://github.com/daiMaruyama)
- Portfolio: [https://daimaruyama.github.io/](https://daimaruyama.github.io/)

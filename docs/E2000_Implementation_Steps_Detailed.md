
# 🚀 実装ステップ詳細 — Exchange API Library

> 本章は D002「親仕様書（詳細版）」の **5. 実装ステップ** を、実務でそのまま運用できる粒度に詳細化する。  
> 4フェーズ（α/β/γ/RC）を軸に、**作業項目・成果物・受入条件（Exit Criteria）・テスト観点・CI/CD** を定義。

---

## 0. 前提と全体方針
- **ブランチ戦略**: `main`（常にデプロイ可能） / `dev`（統合） / `feature/*`（個別） / `release/*`（RC）。
- **コミット規約**: Conventional Commits（`feat:`, `fix:`, `refactor:` 等）。
- **コード規約**: .editorconfig で統一（nullable enable, warn as error など）。
- **ドキュメント規約**: すべての公開I/Fに XML ドキュメントコメント必須。

---

## 1. フェーズ構成（概要）

| フェーズ | 目的 | 範囲 | 主成果物 | Exit Criteria |
|---------|------|------|----------|---------------|
| α | 最小動作の確立 | Rest.Core / Domain / Application（最小） | PlaceMarket 最短経路、Result<T>骨格 | ユースケース1件のE2E成功、基本テスト合格 |
| β | 取引所疎通と主要機能 | Adapter / Protocol の実装 | 発注・照会・取消・残高の共通I/F | Sandboxで4操作が安定動作、エラー正規化 |
| γ | 横断機能の拡張 | Rest.Extension / 可観測性 | ログ/リトライ/JSON/レート制限 | 失敗時の復元性と観測性の基準達成 |
| RC | リリース候補化 | ドキュメント/CI/CD/テスト | パッケージ公開可能な状態 | 全ゲート通過、セマンティックバージョニング確定 |

---

## 2. αフェーズ（Minimum Viable Path）

### 2.1 作業項目
- Rest.Core: `IHttpClient`, `HttpRequest/Response` 実装（タイムアウト/キャンセル）
- Domain: `Order`, `Price`, `Quantity`, `OrderId` など最小型と不変条件
- Application: `IPlaceMarketOrderUseCase` と `IDtoMapper`（ダミー/スタブ）
- Api.Public: `ITradingApi` の雛形
- Result/Error: 共通 `Result<T>` / `ErrorCode` 骨格

### 2.2 成果物
- 最小ユースケース：`PlaceMarket` の**スタブ応答**でのE2E通過
- 単体テスト：Domain（桁・丸め・境界値）/ Result（Map/Fail/Ok）

### 2.3 Exit Criteria
- `PlaceMarket` が `Result<Order>` を返す最短経路の**グリーン**化
- Domain 不変条件テスト**100%（対象範囲内）**
- `main` が常時ビルド成功（CI起動）

---

## 3. βフェーズ（Exchange 通信の実装）

### 3.1 作業項目
- Protocol: `IProtocolSigner`, `IClock`, `IServerClockSync`, `IRateGate`（最小）
- Adapter: 1取引所（例: Binance）で以下を実装
  - `PlaceMarket`, `PlaceLimit`, `Cancel`, `GetBalance`
  - JSON→共通DTO 変換、誤差/丸めルールの反映
  - `IExchangeErrorMap` によるエラーマッピング
- Application: DTO⇔Domain Mapper の本実装
- Integration テスト: サンドボックス/テストネット疎通

### 3.2 成果物
- **4操作**（発注・照会・取消・残高）の共通I/F 完成
- 契約テスト：レスポンス JSON のスナップショット整合
- 疑似障害（Timeout, 5xx, 429）時の基本挙動

### 3.3 Exit Criteria
- サンドボックスで**50回連続の注文→照会→取消**が成功（再現性）
- 全エラーが `ErrorCode` に正規化されログ出力される
- `IExchange*Port` のインターフェースが固まる（Breaking Change 凍結）

---

## 4. γフェーズ（Extension & Observability）

### 4.1 作業項目
- Rest.Extension: `IRetryPolicy`, `ICircuitPolicy`, `IJson`, 構造化ログ/トレース
- Protocol: レート制限の実レート実装（トークンバケット）、時刻ドリフト補正
- Adapter: Idempotency-Key ヘッダ対応／再送規約の組み込み
- メトリクス: 主要KPIのエクスポート（成功率、P50/P95/P99、リトライ回数）

### 4.2 成果物
- 失敗注入テスト（フェイルテスト）: 強制 `429/5xx/Timeout/Network` の回復性
- パフォーマンステスト：スループットと遅延の計測レポート

### 4.3 Exit Criteria（数値目標の例）
- API呼び出し成功率 **≥ 99.9%**（取引所障害除く）
- リトライ後の復旧成功率 **≥ 95%**
- 追加オーバーヘッド：平均遅延 **+≤ 30ms**（Extension導入前比）
- ログ/メトリクスがダッシュボードで可視化可能

---

## 5. RCフェーズ（Release Candidate）

### 5.1 作業項目
- ドキュメント: API リファレンス（XML→Doc生成）、導入ガイド、サンプルコード
- セキュリティ: `ISecretProvider` 組込み、鍵の平文保存禁止の検証
- CI/CD: パッケージ（NuGet）署名、SBOM生成、ライセンススキャン
- 互換性: `Api.Public` / `Adapter.Abstractions` の API フリーズ、SemVer 付与
- リーガル: OSS ライセンス表記、サードパーティコンプライアンス

### 5.2 成果物
- `Exchange.Core` 群 NuGet パッケージ、`Exchange.{Name}` プラグインパッケージ
- サンプルアプリ（CLI or Minimal API）とクイックスタート

### 5.3 Exit Criteria
- 全テスト（Unit/Contract/Integration/E2E/Perf）**グリーン**
- セキュリティ診断項目**ゼロクリティカル**
- 生成物にバージョン・署名・SBOM が付与され配布可能

---

## 6. CI/CD パイプライン（推奨）

1) **Build & Static Analysis**  
   - `dotnet build -c Release`  
   - Roslyn analyzer / StyleCop / ArchUnitNet で層違反検出

2) **Test**  
   - Unit → Contract → Integration（サンドボックス鍵で並列制御）  
   - カバレッジ収集（目標：**80%+**）

3) **Security & Compliance**  
   - SCA（依存脆弱性チェック）、SBOM 生成、ライセンス検証

4) **Package**  
   - `dotnet pack` → NuGet 署名 → リポジトリへ push（`release/*` ブランチ）

5) **Publish Docs**  
   - DocFX or Sandcastle で API ドキュメントを生成し公開

---

## 7. テンプレートと雛形

### 7.1 フィーチャー雛形（Adapter）
```
/Exchange.{Name}/
  /Endpoints/
  /Mappers/
  /Errors/
  {Name}TradingPort.cs
  {Name}AccountPort.cs
  {Name}Options.cs
```

### 7.2 Issue テンプレート（抜粋）
- **Title**: `[feat][adapter:{Name}] PlaceLimit 実装`
- **Definition of Ready**:
  - エンドポイント仕様URL
  - レスポンスJSONサンプル
  - エラーテーブル
- **Definition of Done**:
  - 契約テスト作成・緑化
  - 観測値（P50/P95/P99）記録

---

## 8. リスクと緩和策

| リスク | 緩和策 |
|-------|-------|
| 取引所の仕様変更 | Adapter 層に閉じ込め、契約テストのスナップショット差分で検知 |
| 時刻ずれによる署名失敗 | Protocol の ServerTime 同期と `recvWindow` 調整 |
| レート制限超過 | RateGate による協調制御、バックオフ調整 |
| JSON 互換性問題 | 取引所別コンバータと緩やかなデシリアライザ |

---

## 9. 受入チェックリスト（Phase Gate）

- [ ] `main` が常時ビルド & テストパス
- [ ] `Api.Public` / `Adapter.Abstractions` の破壊変更なし
- [ ] 主要ユースケース（4操作）の回帰テストパス
- [ ] 可観測性（ログ・メトリクス・トレース）が有効
- [ ] セキュリティ（鍵/秘密/署名）基準に抵触なし

---

## 付録A) サンプルコマンド
```bash
# Build & Test
dotnet build -c Release
dotnet test -c Release /p:CollectCoverage=true

# Pack (pre-release)
dotnet pack src/Exchange.Core/Exchange.Core.csproj -c Release -o ./artifacts /p:PackageVersion=0.1.0-alpha

# Local install
dotnet nuget add source ./artifacts --name local
```


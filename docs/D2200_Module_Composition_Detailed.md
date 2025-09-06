# D2200\_Module\_Composition\_Detailed — モジュール構成（詳細設計・改訂版）

**版**: 2.0（2025-09-04）\
**対象**: ExchangeApi ソリューション\
**要約**: 本書は ExchangeApi の **モジュール（プロジェクト）構成**を確定し、**7層アーキテクチャ**と整合した**依存規約**・**配置規約**・**命名**・**テスト方針**を定義する。既存文書（D2100, E2200）との差異を解消し、以降の実装・レビュー・CIの基準とする。

---

## 0. 目的・非目的

- **目的**: 7層の責務を**プロジェクト単位**に落とし込み、依存方向をコードベースで担保する。
- **非目的**: 取引戦略ロジック（売買アルゴリズム）の実装・評価。本ライブラリは**Exchange API の抽象化**が責務。

---

## 1. 層モデル（D2100 整合）

> 7層＝**縦の層**。この他に **Abstractions**（サイドカー契約パッケージ）を横付けするが、**層には数えない**。

```
UI  →  Application  →  Domain  →  Adapter  →  Protocol  →  Rest.Extension  →  Rest.Core
↑            ↑          ↑           ↑            ↑                 ↑
└──────────────  Abstractions（依存ゼロ：IClock/Result など契約のみ）
```

- **UI**: 公開エンドポイント（CLI/Minimal API/サンプル）。入出力整形のみ。
- **Application**: ユースケース指揮、DTO↔Domain 変換、Ports 公開、トランザクション境界。
- **Domain**: 取引所共通のドメインモデル（Order/Balance/Ticker 等）。**戦略ロジックは含めない**。
- **Adapter**: 取引所固有 REST/WS ↔ 共通DTO 変換、例外/エラーコードのマッピング。
- **Protocol**: 署名・認証・時刻同期・レート制御・Idempotency-Key 等のプロトコル処理。
- **Rest.Extension**: リトライ/バックオフ/サーキットブレーカ/ロギング/メトリクス等の横断機能。
- **Rest.Core**: HTTP I/O・JSON シリアライズ・タイムアウト・Cancellation の最下層プリミティブ。
- **Abstractions**: **契約だけ**（interface/enum/小さな結果型）。**実装と外部依存は禁止**。

---

## 2. プロジェクト構成（確定）

```
ExchangeApi.UI                // サンプル/CLI/Minimal API（Composition Root）
ExchangeApi.Application       // UseCase / Ports / DTO↔Domain Assembler / Validation
ExchangeApi.Domain            // 共通ドメインモデル（戦略なし）
ExchangeApi.Adapter           // 取引所Adapter基盤 + Integrations/*
ExchangeApi.Protocol          // 署名/時刻/レート制御
ExchangeApi.Rest.Extension    // Polly等のポリシー/計測/構造化ログ
ExchangeApi.Rest.Core         // Http/JSONの基盤（最下層）
ExchangeApi.Abstractions      // 契約サイドカー（依存ゼロ）
```

### 2.1 各プロジェクトの責務・許可依存

| プロジェクト         | 主責務                                 | 許可依存                                    | 禁止事項                                          |
| -------------- | ----------------------------------- | --------------------------------------- | --------------------------------------------- |
| UI             | 入出力整形・DI構成・起動                       | Application, Abstractions               | Domain/Adapter/Rest.\* 直接参照。ビジネス判断の実装         |
| Application    | ユースケース、Ports、DTO↔Domain、Validation  | Domain, Abstractions                    | Adapter/Protocol/Rest.\* への直接依存、取引所Wire DTO参照 |
| Domain         | 共通モデル/値オブジェクト/ルール                   | Abstractions                            | HTTP/JSON/Polly/時刻実装への依存、Wire DTO参照           |
| Adapter        | 取引所REST/WS実装、Wire DTO、マッピング         | Protocol, Rest.Extension, Abstractions  | Application/Domain参照、UI参照                     |
| Protocol       | 署名/認証/時刻同期/レート制御                    | Rest.Extension, Rest.Core, Abstractions | Application/Domain/Adapter参照                  |
| Rest.Extension | リトライ/サーキット/ログ/メトリクス                 | Rest.Core, Abstractions                 | 上位層参照、取引所依存                                   |
| Rest.Core      | Http/Json/タイムアウト/CT                 | Abstractions                            | 上位層参照、Polly直使用（Extension経由に統一）                |
| Abstractions   | IClock/IdFactory/Result/ErrorCode 等 | （なし）                                    | 外部NuGet/実装/DTO/ビジネス型                          |

> **原則**: 参照は**上→下のみ**。Abstractions は**どこからでも参照可**だが、自身は**何も参照しない**。

---

## 3. 依存規約（必読）

- **R1**: 参照方向は *UI→…→Rest.Core* の **一方向**。逆参照禁止。
- **R2**: **Wire DTO**（取引所のJSON表現）は **Adapter/Protocol** に閉じ込める。
- **R3**: **Assembler** による **DTO↔Domain 変換**は **Application** に置く。
- **R4**: **Domain は外部技術を知らない**（HTTP/JSON/Config/Polly/時計実装を参照しない）。
- **R5**: **Abstractions は契約のみ**（実装/外部依存を入れない）。
- **R6**: **Retry/Timeout/Logging** は Rest.Extension 経由に統一（各層で勝手に実装しない）。

---

## 4. 命名・名前空間・配置規約

- プロジェクト名 = 層名。例：`ExchangeApi.Application`。
- 名前空間は `ExchangeApi.{Layer}[.SubArea]` を基本。例：`ExchangeApi.Application.Ports`。
- Adapter 直下に `Adapters.Bybit`, `Adapters.Binance` 等の**サブ名前空間/フォルダ**を配置。
- Application の公開 DTO は `ExchangeApi.Application.Contracts` に集約。
- テストは `{Project}.Tests`（同階層）に分離。

---

## 5. DTO/Domain/Wire の境界

- **Application DTO**（公開契約）: UI/外部に見せる形。versioning 可能。例：`PlaceOrderRequestDto`。
- **Domain Model**（内部厳密）: 値オブジェクト/不変条件/単位。例：`Quantity`, `Price`, `Order`。
- **Wire DTO**（取引所JSON）: Adapter/Protocol 内部のみ。外部流出禁止。

> 変換責務: **Application.Assemblers** で **DTO→Domain**、**Adapter/Protocol**で **Wire→Domain**。

---

## 6. Ports（Application）

- 例: `IOrderPort`, `IAccountPort`, `IMarketDataPort`（**Abstractionsではなく Application に置く**）。
- Adapter は **Ports を実装**する（DIで差し替え可能に）。

---

## 7. Abstractions（サイドカー）

- 含めてよい: `IClock`, `IIdFactory`, `Result<T>`, `ErrorCode`, （依存ゼロ属性）。
- 含めない: DTO/Domain/取引所Wire/実装/外部依存が必要な型。
- csproj は **PackageReference 禁止**。System.\* のみ。

---

## 8. 外部ライブラリの配置基準

- **Rest.Core**: `System.Net.Http`, `System.Text.Json` など**BCL中心**。
- **Rest.Extension**: Polly, OpenTelemetry/ILogger 等の**横断機能**。
- **Adapter/Protocol**: 必要な場合のみ軽量HTTPクライアント等。**UI/Application/Domain は外部ライブラリを直接参照しない**。

---

## 9. テスト戦略

- **Unit**
  - Application: ユースケース単位（Ports をモック）。DTO↔Domain 変換の検証。
  - Domain: 値オブジェクトの不変条件/計算ロジック。
  - Protocol: 署名/時刻ズレ/レート制御の境界条件。
  - Adapter: **契約テスト**（Ports 実装としての挙動一致）。Wire→Domain マッピングの同値性。
- **Integration**
  - Rest.Extension↔Rest.Core のポリシ連携、HTTPのタイムアウト/リトライ。
- **End-to-End (sample)**
  - UI から Minimal API/CLI 経由で主要ユースケースを 1 本通す（モック取引所）。

---

## 10. CI ガード（層崩れ防止）

- **G1 参照方向検査**: `.csproj` を静的解析し、逆方向参照が存在すればビルド失敗。
- **G2 禁輸型検査**: Roslyn Analyzer（またはカスタム）で Domain から `HttpClient`/`Json*` 使用を検出。
- **G3 Abstractions 純度検査**: `ExchangeApi.Abstractions.csproj` に `PackageReference` が追加されたら失敗。
- **G4 Wire漏れ検査**: `Adapters.*` 名前空間の型が Application/Domain に現れたら失敗。

---

## 11. バージョニング/配布

- プロジェクトごとに **SemVer** を採用。ブレイキング変更は **Application DTO** のメジャー更新で通知。
- NuGet 配布対象（想定）: `Application`, `Domain`, `Adapter`（基盤）, `Protocol`, `Rest.Extension`, `Rest.Core`, `Abstractions`。
- `Directory.Packages.props` でバージョンを集中管理。依存の**上振れ更新をCIで禁止**。

---

## 12. 既存構成とのマッピング（移行計画）

| 旧/現行                         | 新構成                                 | 移行方針                                         |
| ---------------------------- | ----------------------------------- | -------------------------------------------- |
| ExchangeApi.Applications     | **ExchangeApi.Application**         | 名前と名前空間を統一。Ports をここへ集約                      |
| ExchangeApi.Domain           | **ExchangeApi.Domain**              | 変更なし（戦略ロジックは含めない）                            |
| ExchangeApi.AbstractExchange | **廃止** → Application.Ports 吸収       | 型名は継続可。名前空間を `Application.Ports` に変更         |
| ExchangeApi.Rest.Core        | **ExchangeApi.Rest.Core**           | 変更なし（実装のみ）                                   |
| ExchangeApi.Rest.Adapter     | **ExchangeApi.Adapter**             | プロジェクト名変更。Integrations/\* を配下へ整理             |
| ExchangeApi.Foundation       | **分割** → Rest.Extension / Rest.Core | `Result<T>`/`IClock` 類は Abstractions へ、実装は下へ |
| ExchangeApi.Integrations.\*  | **Adapter/Adapters.**\*             | フォルダ/名前空間整理（Bybit, Binance 等）                |
| samples / Cli                | **ExchangeApi.UI**                  | Composition Root として統合                       |

---

## 13. 参照例（csproj 抜粋）

```xml
<!-- Application.csproj -->
<ItemGroup>
  <ProjectReference Include="..\ExchangeApi.Domain\ExchangeApi.Domain.csproj" />
  <ProjectReference Include="..\ExchangeApi.Abstractions\ExchangeApi.Abstractions.csproj" />
</ItemGroup>
```

```xml
<!-- Adapter.csproj -->
<ItemGroup>
  <ProjectReference Include="..\ExchangeApi.Protocol\ExchangeApi.Protocol.csproj" />
  <ProjectReference Include="..\ExchangeApi.Rest.Extension\ExchangeApi.Rest.Extension.csproj" />
  <ProjectReference Include="..\ExchangeApi.Abstractions\ExchangeApi.Abstractions.csproj" />
</ItemGroup>
```

---

## 14. DI（Composition Root 例：UI）

```csharp
// Abstractions の契約に対して実装を束ねる（下位実装は UI だけが知る）
services.AddSingleton<IClock, SystemClock>();       // Rest.Core 実装
services.AddSingleton<IIdFactory, GuidIdFactory>(); // Rest.Core 実装

// Ports 実装（Adapter 側）
services.AddScoped<IOrderPort, BybitOrderAdapter>();
services.AddScoped<IMarketDataPort, BybitMarketDataAdapter>();

// UseCase
services.AddScoped<OrderAppService>();
```

---

## 15. 完了基準（Definition of Done）

- 7層に対応する **7プロジェクト + Abstractions** が作成され、**相互参照が規約どおり**。
- CI の **G1〜G4** が導入され、層崩れがビルドで検出される。
- Application の **Ports** と **Assembler** が雛形として配置されている。
- Adapter に **Integrations/**\* が配置され、少なくとも **Mock** が動作する。
- UI に **最小サンプル**（OHLC取得→成行/指値→残高/ポジション確認）があり、起動確認済み。

---

## 付録 A: よくある質問（抜粋）

**Q1. Abstractions を層に入れない理由は？**\
A. 実装や外部依存を持たない“契約”だけだから。どの層にも属さず、逆流防止のためのサイドカー。

**Q2. Foundation を残してはダメ？**\
A. 何でも入る箱になりやすく、層崩れの温床。Abstractions と Rest.Extension/Rest.Core へ機能分解する。

**Q3. Domain に戦略ロジックを入れないのはなぜ？**\
A. 本ライブラリの責務は「取引所 API の抽象化」。戦略は**利用者のアプリケーション**に属するため。


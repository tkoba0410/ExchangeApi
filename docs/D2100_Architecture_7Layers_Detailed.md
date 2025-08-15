# 🏗️ アーキテクチャ（7層）詳細設計 — Exchange API Library

> 本章は「親仕様書（詳細版） D002」の **2. アーキテクチャ（7層）** を実装可能な粒度まで詳細化したもの。\
> C# を前提とし、層境界・依存規約・I/F・エラーモデル・テスト容易性・可観測性を定義する。

---

## 0. 全体原則

- **依存方向は下位層のみ**（UI→Application→Domain→Adapter→Protocol→Rest.Extension→Rest.Core）。
- **循環依存禁止**、**静的参照の一方向性**を `Directory.Packages.props`／ソリューション構成で担保。
- 例外は**境界内でのみ**捕捉し、境界外へは `Result<T>` と `ErrorCode`（列挙）で伝播。
- **CancellationToken** 必須、**タイムアウトは呼び出し側指定が優先**。
- **Clock / Guid / Random / Http** は必ず抽象化（`IClock`, `IIdFactory`, `IHttpClient`）。
- **Idempotency-Key** を注文系 I/F のヘッダに標準化（再試行の重複約定を防止）。

```
[ UI / API ]         : 外部公開I/F、アプリや外部サービスから利用
    ↓
[ Application ]      : ユースケース制御、Mapper、Result生成
    ↓
[ Domain.Model / Dto ] : エンティティ、値オブジェクト、DTO定義
    ↓
[ Adapter.XYZ ]      : 取引所固有のAPI実装、レスポンス変換
    ↓
[ Protocol ]         : 署名、認証ヘッダ生成、時刻同期、レート制御
    ↓
[ Rest.Extension ]   : ログ、リトライ、JSON共通処理、ポリシー
    ↓
[ Rest.Core ]        : HTTPクライアント基盤、低レベル通信
```

---

## 1. 層別仕様

### 1.1 UI / API 層（公開I/F）

- **責務**: 外部クライアントが呼ぶ安定I/F。DTO受け渡し・認可（必要なら）・入力検証（軽量）。
- **入出力**: `Application` の UseCase を注入して呼び出す。戻りは `Result<T>`。
- **禁則**: `Adapter` や `Rest.*` へ直接参照禁止。
- **例**:

```csharp
public interface ITradingApi {
    Task<Result<OrderResponseDto>> PlaceMarketAsync(PlaceMarketDto req, CancellationToken ct);
    Task<Result<CancelResponseDto>> CancelAsync(CancelDto req, CancellationToken ct);
    Task<Result<AccountBalanceDto>> GetBalanceAsync(string accountId, CancellationToken ct);
}
```

#### 1.1.1 公開インターフェース設計方針（共通 / 固有）

本ライブラリの公開APIは、以下の2層構造で提供する。

**共通インターフェース（IExchangeApi）**

- すべての取引所で利用可能な基本機能を統一仕様で提供する。
- 発注（成行/指値）、注文照会、注文キャンセル、残高取得、市場データ取得など主要な操作をカバー。
- 実装は各取引所Adapterが共通DTO・共通ErrorCodeを通じて行う。
- 共通I/Fのみで主要な資産管理・取引操作が動作することを保証する。

**取引所固有インターフェース（IExchangeApi\_{ExchangeName}）**

- 共通化が困難な固有機能を提供する。
  - 特殊注文タイプ（Iceberg, TWAP, VWAP など）
  - 特殊マーケット（Perpetual, Options, Futures固有API）
  - ステーキング、ローンチパッド、上場/廃止スケジュール取得 等
- IExchangeApiを継承し、固有のメソッドを追加する形で定義する。

**利用方針**

- 利用者は共通I/Fを通じて取引所差異を意識せずに基本機能を利用できる。
- 必要な場合のみ、固有I/Fへ型キャストして固有機能を呼び出す。
- 戻り値・例外型は共通定義（DTO、ErrorCode）を必ず使用する。
- 固有I/Fは必須利用を前提とせず、利用しなくても全主要機能が動作可能であること。

### 1.2 Application 層

- **責務**: ユースケースのオーケストレーション。DTO⇔Domain 変換、トランザクション境界（論理）。
- **依存**: `Domain`, `Adapter`（抽象I/Fのみ）。
- **設計**:
  - UseCase は**入出力 DTO と **``** を返す非同期メソッド**を1件ずつ提供。
  - **Mapper** は明示注入（AutoMapperなど外部ライブラリに直依存しないファサードを介す）。
- **例**:

```csharp
public sealed class PlaceMarketOrderUseCase : IPlaceMarketOrderUseCase {
    private readonly IExchangeTradingPort _trading; // Adapter抽象
    private readonly IDtoMapper _mapper;
    public async Task<Result<OrderResponseDto>> HandleAsync(PlaceMarketDto dto, CancellationToken ct) {
        var cmd = _mapper.ToDomain(dto);
        var res = await _trading.PlaceMarketAsync(cmd, ct);
        return res.Map(_mapper.ToDto);
    }
}
```

### 1.3 Domain.Model / Dto 層

- **責務**: 取引所非依存のビジネスルール・型安全。通貨・数量の検証、丸め、桁制約。
- **主構成**: `Order`, `Symbol`, `Money(Amount,Currency)`, `Price`, `Quantity`, `OrderId`, `Exchange`（VO）等。
- **不変条件**: `Quantity > 0`, `Price ≥ 0`, 通貨桁数遵守（例: BTC 8桁）。
- **DTO**: UseCase I/O のワイヤ形式。**Domain 直露出禁止**。
- **例**:

```csharp
public readonly record struct Quantity(decimal Value) {
    public Quantity { if (Value <= 0) throw new DomainException("Qty>0"); }
}
```

### 1.4 Adapter 層（取引所プラグイン）

- **責務**: 取引所固有のエンドポイント呼び出し・JSON→共通DTO変換・誤差/丸め規則の反映。
- **依存**: `Protocol`（署名/時刻同期）、`Rest.Extension`（ポリシー/JSON）、`Rest.Core`。
- **拡張性**: 取引所ごとに `Exchange.Binance`, `Exchange.Bybit` など**別パッケージ**で提供。
- **抽象ポート**: `IExchangeTradingPort`, `IExchangeAccountPort`, `IExchangeMarketDataPort`。
- **エラーマッピング**: 取引所コード→共通 `ErrorCode`（例: `InvalidNonce`, `InsufficientFunds`）。
- **例**:

```csharp
public interface IExchangeTradingPort {
    Task<Result<Order>> PlaceMarketAsync(PlaceMarket cmd, CancellationToken ct);
    Task<Result<Order>> PlaceLimitAsync(PlaceLimit cmd, CancellationToken ct);
    Task<Result<Unit>> CancelAsync(OrderId id, CancellationToken ct);
}
```

### 1.5 Protocol 層

- **責務**: 署名（HMAC/EdDSA等）、認証ヘッダ生成、**時刻同期（サーバ時刻差の補正）**、 **レート制限の協調制御**（トークンバケット／内製カウンタ）。
- **入出力**: `Sign(request) -> AuthHeader` / `SyncClock()` / `RateGate.WaitAsync()`。
- **注意**: **決してビジネスを持たない**。再利用可能な純粋技術ユーティリティ群。

### 1.6 Rest.Extension 層

- **責務**: 横断関心事ポリシーの集中実装。
  - **リトライ**（指数バックオフ+ジッタ、再送安全メソッドのみ）
  - **サーキットブレーカ**
  - **タイムアウト委譲**
  - **構造化ログ**（RequestId, CorrelationId, Elapsed）
  - **JSON**（シリアライズ/デシリアライズ設定ポリシー）
- **I/F**: `IRetryPolicy`, `ICircuitPolicy`, `IJson`, `ILogger`（抽象）。

### 1.7 Rest.Core 層

- **責務**: HTTP 基盤の最小カーネル。
  - `IHttpClient`（`SendAsync(HttpRequest, ct)`）
  - 接続プール、DNS更新、TLS設定
  - 生リクエスト/レスポンスの取り扱い（ボディは `ReadOnlyMemory<byte>` / `Stream`）
- **禁則**: ドメイン型・DTO を知らない。純粋なtransport。

---

## 2. 依存規約とディレクトリ

```
/src
  /Api.Public                -> UI/API
  /Application              -> UseCases, Mappers, Result
  /Domain                   -> Entities, VOs, DTOs
  /Protocol                 -> Signing, Clock, RateGate
  /Rest.Core                -> Http Abstraction
  /Rest.Extension           -> Retry, CB, Json, Logging
  /Exchange.Binance         -> Adapter Impl (例)
  /Exchange.Bybit           -> Adapter Impl (例)
/tests
  /Application.Tests        -> UseCase単体
  /Domain.Tests             -> ルール検証
  /Adapter.Tests            -> 取引所モック疎通
  /Integration.Tests        -> E2E（サンドボックス）
```

- **参照のみ許可**: 上位→下位。`Domain` はどこにも依存しない。
- **NuGet 分割**: `Exchange.*` はプラグイン。コアから分離して配布。

---

## 3. 共通契約（Result/エラー/ポリシー）

### 3.1 Result / Error

```csharp
public enum ErrorCode {
    None = 0,
    Network, Timeout, Unauthorized, RateLimited,
    InvalidRequest, InvalidNonce, InsufficientFunds,
    ExchangeDown, Deserialization, Unknown
}
public readonly record struct Error(ErrorCode Code, string Message, string? Detail = null);
public readonly record struct Result<T>(bool IsSuccess, T? Value, Error? Error) {
    public static Result<T> Ok(T v) => new(true, v, null);
    public static Result<T> Fail(Error e) => new(false, default, e);
    public Result<U> Map<U>(Func<T, U> f) => IsSuccess ? Result<U>.Ok(f(Value!)) : Result<U>.Fail(Error!.Value);
}
```

### 3.2 リトライ・タイムアウト・冪等性

- **再送可**: `GET`/照会、**Idempotency-Key** を付与した `POST` のみ。
- **バックオフ**: 100ms 基点、係数 2.0、ジッタ ±20%、最大 5 回。
- **総合タイムアウト**: 呼出し側 `CancellationToken` と統合。

---

## 4. テスト容易性とモックポイント

- **UI / API**: 直呼びで入出力 DTO の検証。
- **Application**: `IExchangeTradingPort` 等を**モック注入**してユースケース検証。
- **Domain**: 不変条件のプロパティテスト（境界値／桁数）。
- **Adapter**: 取引所レスポンスの**スナップショットテスト**（JSON→共通DTO）。
- **Protocol**: 署名ベクトル（既知入力→既知署名）テスト。
- **Rest.Extension/Core**: ポリシーの**時間依存テスト**は `IClock` で制御。

---

## 5. 可観測性（Observability）

- **構造化ログ**: `CorrelationId`, `RequestId`, `Exchange`, `Endpoint`, `HttpStatus`, `ErrorCode`, `DurationMs`。
- **メトリクス**: 成功率、P50/P95/P99、リトライ回数、サーキット開閉、レート制限待ち時間。
- **トレース**: 各層で `Activity` を開始し、親子関係を維持。

---

## 6. セキュリティと設定

- **秘密情報**: API キーは安全ストアから `ISecretProvider` 経由で取得。平文ファイル禁止。
- **時刻**: `IClock.UtcNow` を唯一の時間源。サーバ時刻との差分を Protocol 層で補正。
- **設定**: `IOptions<T>` ファサードで注入。例：`HttpOptions`, `RetryOptions`, `RateLimitOptions`。

---

## 7. 例外とバウンダリ

- **内部例外**は**層内で回収**し `ErrorCode` に正規化。外部へ .NET 例外を漏らさない。
- **致命的例外**（Argument/Config）は起動時の Fail-Fast とし、運用時は起こさない。

---

## 8. 代表I/F一覧（抜粋）

```csharp
public interface IExchangeAccountPort {
    Task<Result<AccountBalance>> GetBalanceAsync(AccountId id, CancellationToken ct);
}
public interface IRateGate {
    ValueTask WaitAsync(string key, CancellationToken ct);
}
public interface IProtocolSigner {
    SignedRequest Sign(HttpRequestMessage req, ApiKey key, in Nonce nonce);
}
public interface IHttpClient {
    Task<HttpResponse> SendAsync(HttpRequest request, CancellationToken ct);
}
```

---

## 9. 拡張ポイント

- **新規取引所追加**: `Exchange.{Name}` パッケージを作成し `*Port` 実装とレスポンス→共通DTO マッピングを追加。
- **機能追加**: 新ユースケースは Application に UseCase、Domain に型、Adapter に対応メソッドを順に追加。
- **WebSocket**（将来）: `Transport.WebSocket` を `Rest.Core` と並置し、Protocol/Extension のポリシーを共有。

---

## 10. 品質ゲート（アーキテクチャ観点）

- 静的解析で**禁止参照**検出（ArchUnitNet 等）。
- パッケージ単体でテスト可能（上位に依存しない）。
- 主要I/Fは XML ドキュメントコメント必須（外部公開品質）。


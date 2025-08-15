# 📦 モジュール構成詳細 — Exchange API Library（改訂ドラフト v3 固有API命名方針追記）

> 本章はアーキテクチャ（7層）を踏まえ、**パッケージ境界/依存関係/公開I/F/設定/テスト戦略/拡張ポイント**を、 **共通I/F（ExchangeClient）と取引所固有I/F（IExchangeApi\_{Name}）の二層公開方針**に沿って再定義する。 言語は C#/.NET 前提。各モジュールは **独立配布（NuGet化）** を想定。

---

## 0. 目次

1. パッケージ一覧と依存関係（改訂：サブクライアント方式）
2. 各モジュール仕様（公開I/F・代表型・設定・ログ/メトリクス）
   - 2.1 ExchangeClient（ファサード：取引所サブクライアント公開）
   - 2.2 ExchangeClient.\*（取引所別サブクライアント：Binance/Bybit…）
   - 2.3 固有API命名方針
   - 2.4 Application
   - 2.5 Domain
   - 2.6 Adapter.Abstractions
   - 2.7 Protocol
   - 2.8 Rest.Core
   - 2.9 Rest.Extension
   - 2.10 Exchange.\*（アダプタ実装）
3. 例外・エラー正規化と戻り値規約（変更なし/補足）
4. DI 構成とブートストラップ（改訂）
5. 設定（Options）と環境変数（補足）
6. テスト戦略（改訂）
7. バージョニング/互換性ポリシー（改訂）
8. 変更容易性の設計支点（Extension Points）（補足）
9. 付録：サンプルコード（client.Binance.GetTicker の利用）

---

## 1) パッケージ一覧と依存関係（改訂：サブクライアント方式）

```
ExchangeClient                      -> ExchangeClient.Abstractions
ExchangeClient.Abstractions         -> (依存なし)
ExchangeClient.Binance              -> ExchangeClient.Abstractions, Adapter.Abstractions, Protocol, Rest.Extension, Rest.Core
ExchangeClient.Bybit                -> ExchangeClient.Abstractions, Adapter.Abstractions, Protocol, Rest.Extension, Rest.Core

Application                         -> Domain, Adapter.Abstractions
Domain                              -> (依存なし)
Adapter.Abstractions                -> Domain, Protocol, Rest.Extension, Rest.Core
Protocol                            -> Rest.Extension, Rest.Core
Rest.Extension                      -> Rest.Core
Rest.Core                           -> (依存なし)

Exchange.Binance                    -> Adapter.Abstractions, Protocol, Rest.Extension, Rest.Core
Exchange.Bybit                      -> Adapter.Abstractions, Protocol, Rest.Extension, Rest.Core
```

- **公開方針**

  - `ExchangeClient` は **ファサード**。取引所ごとの **サブクライアント**（`ExchangeClient.Binance`, `ExchangeClient.Bybit`, …）を公開プロパティで提供。
  - サブクライアントは **共通アダプタ抽象（Adapter.Abstractions）** に依存し、実装は各 `Exchange.{Name}` と連携。
  - `ExchangeClient.Abstractions` には下記の **公開I/F**（`IExchangeClient`, `IBinanceClient`, `IBybitClient` …）のみを格納。

- **禁止**: 上位→下位以外の参照、循環依存、`Domain` からの外部参照。

- **配布**: `ExchangeClient.*` は SDK 層として NuGet 配布。`Exchange.*` はアダプタ層として独立配布。

---

## 2) 各モジュール仕様

> 設計原則: 本SDKはフルエント（メソッドチェーン）を**採用しない**。すべてのAPIは**引数で完全指定する単一メソッド**として提供する。呼び出し形の統一と学習コスト低減、並列安全性・副作用排除のため。（概要）

### 2.1 ExchangeClient（ファサード：取引所サブクライアント公開）

```csharp
public interface IExchangeClient
{
    IBinanceClient Binance { get; }
    IBybitClient Bybit { get; }
    IExchangeMetadata Metadata { get; }
}

public interface IExchangeMetadata
{
    string Version { get; }
    string Runtime { get; }
}

public sealed class ExchangeClient : IExchangeClient
{
    public IBinanceClient Binance { get; }
    public IBybitClient Bybit { get; }
    public IExchangeMetadata Metadata { get; }

    public ExchangeClient(IBinanceClient binance, IBybitClient bybit, IExchangeMetadata meta)
    {
        Binance = binance;
        Bybit = bybit;
        Metadata = meta;
    }
}
```

### 2.2 ExchangeClient.\*（取引所別サブクライアント）

```csharp
public interface IBinanceClient
{
    // 共通API（C#命名規則準拠）
    Task<Result<TickerDto>> GetTickerAsync(string symbol, CancellationToken ct);

    // 固有API（公式エンドポイント名準拠）
    Task<Result<FundingRateDto>> fapiV1FundingRateAsync(string symbol, CancellationToken ct);
}
```

---

## 2.3 固有API命名方針

- **対象**: 各取引所の固有API（共通化が困難なエンドポイント）
- **命名規則無視**: 公式ドキュメントのコマンド文字列やエンドポイント名をそのままメソッド名として採用
- **非同期サフィックス**: 非同期メソッドである場合は `Async` を末尾に付与
- **大文字小文字**: 公式仕様に準拠（PascalCaseやcamelCaseを変換しない）
- **パラメータ名**: 公式パラメータ名をそのまま使用（型はC#準拠）
- **XMLコメント**: 公式エンドポイントURL、HTTPメソッド、認証要否を明記

例:

```csharp
// Binance固有API: /fapi/v1/fundingRate
Task<Result<FundingRateDto>> fapiV1FundingRateAsync(string symbol, CancellationToken ct);
```

---

## 3) 例外・エラー正規化と戻り値規約（補足）

- **原則**: 例外は層内で回収し、外部へは `Result<T>`／`ErrorCode` を返す。
- 共通/固有問わず `ErrorCode` とログフィールドを統一。

---

## 4) DI 構成とブートストラップ（改訂）

```csharp
var services = new ServiceCollection();
services.AddSingleton<IHttpClient, SocketsHttpClient>();
services.AddSingleton<IJson, SystemTextJson>();
services.AddSingleton<IRetryPolicy, ExponentialBackoffPolicy>();
services.AddSingleton<IClock, SystemClock>();
services.AddSingleton<IRateGate, TokenBucketRateGate>();
services.AddSingleton<IProtocolSigner, HmacSigner>();
services.AddSingleton<IBinanceClient, BinanceClient>();
services.AddSingleton<IBybitClient, BybitClient>();
services.AddSingleton<IExchangeMetadata, SdkMetadata>();
services.AddSingleton<IExchangeClient, ExchangeClient>();
```

---

## 9) 付録：サンプルコード（client.Binance.GetTicker の利用）

```csharp
IExchangeClient client = serviceProvider.GetRequiredService<IExchangeClient>();
var ticker = await client.Binance.GetTickerAsync("BTCUSDT", ct);
var funding = await client.Binance.fapiV1FundingRateAsync("BTCUSDT", ct);
```



---

## 10) 固有APIの命名規則（公式準拠モード）

> 取引所固有APIは**公式ドキュメントのコマンド/エンドポイント名をそのままメソッド名に採用**し、C#の一般的な命名規則は**意図的に適用しない**。

### 10.1 ポリシー

- **メソッド名**: 公式名をそのまま使用（大文字小文字/記号含む）。非同期は `Async` を末尾付与。
  - 例: `fapiV1FundingRateAsync`, `sapiV1StakingPositionsAsync`
- **引数名**: 公式パラメータ名を踏襲（例: `symbol`, `recvWindow`, `timestamp`）。
- **戻り**: 既定は `Task<Result<T>>`（共通`ErrorCode`で正規化）。
- **ドキュメンテーション**: XML ドキュメントコメントに**対応エンドポイントURL/HTTPメソッド/認証要否/レート制限区分**を必須記載。
- **名前規則の抑制**: `EditorConfig`/アナライザーで該当ルールを抑制（例: `CA1707`, `IDE1006`）。
  - サンプル: `.editorconfig`
    ```ini
    dotnet_diagnostic.IDE1006.severity = none
    dotnet_diagnostic.CA1707.severity = none
    ```
- **配置**: 固有APIは取引所サブクライアント（例: `IBinanceClient`）にのみ定義。共通I/Fへは移さない。
- **互換性**: 公式名準拠のため、**名称変更は破壊変更**として扱う（Major のみ）。

### 10.2 例（Binance 固有）

```csharp
/// <summary>
/// GET /fapi/v1/fundingRate
/// 認証: 不要, Rate: 5/1s
/// </summary>
Task<Result<FundingRateDto>> fapiV1FundingRateAsync(string symbol, CancellationToken ct);

/// <summary>
/// POST /sapi/v1/staking/position
/// 認証: 必須(HMAC), Rate: 1/1s
/// </summary>
Task<Result<StakingPositionDto>> sapiV1StakingPositionAsync(string product, string asset, CancellationToken ct);
```

### 10.3 オプション: エイリアス（任意）

- 学習コスト低減のため、**C#慣習名の薄いラッパ**を同居させても良い。
  - 例: `GetFundingRateAsync(...) => fapiV1FundingRateAsync(...)`（Obsolete 非推奨または `EditorBrowsable(Never)` で露出制御可）。
- ドキュメントでは**公式名を第一**に案内し、エイリアスは補助とする。

### 10.4 テスト/レビュー観点

- 公式サンプルとの**名称一致**チェック（スナップショット）。
- 署名・時刻同期・レート制限が**公式要件に一致**していること。
- 命名規則抑制の設定が**プロジェクト境界内に限定**されていること（他レイヤへの波及禁止）。


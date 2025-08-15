
# 🔧 共通処理配置詳細 — Exchange API Library

> 本章は D002「親仕様書（詳細版）」の **4. 共通処理配置詳細** を実装粒度で定義する。  
> 横断関心（HTTP/ログ/リトライ/JSON/DTO変換/署名とヘッダ/エラーマップ/時刻同期/レート制限/Idempotency）を、**どの層が責務を持ち、どのI/Fで提供し、どう設定するか**を明確化する。  
> 対象言語は C#/.NET。

---

## 0. 原則（Ownership & Boundaries）

| 機能                         | 所属層            | 主要I/F/型                          | 備考 |
|------------------------------|-------------------|-------------------------------------|------|
| HTTP送受信                   | Rest.Core         | `IHttpClient`                       | Transportの最小核。DTO/Domainを知らない |
| JSON（シリアライズ/逆）      | Rest.Extension    | `IJson`                             | UTF-8固定、数値丸め/日付の方針を統一 |
| リトライ/サーキット/タイムアウト | Rest.Extension    | `IRetryPolicy` / `ICircuitPolicy`   | 冪等操作のみ再試行 |
| 構造化ログ/トレース          | Rest.Extension    | `ILogger` / `Activity`              | CorrelationId/RequestIdを全層で伝播 |
| 署名/認証ヘッダ              | Protocol          | `IProtocolSigner`                   | 取引所ごとの署名方式に拡張点 |
| 時刻同期/スキュー補正        | Protocol          | `IServerClockSync`, `IClock`        | +/-許容範囲をOptionsで管理 |
| レート制限（協調・局所）     | Protocol          | `IRateGate`                         | 取引所・エンドポイント単位のキー |
| エラーマップ（正規化）       | Adapter           | `IExchangeErrorMap`                 | Exchange固有コード→共通`ErrorCode` |
| DTO⇔Domain 変換              | Application       | `IDtoMapper`                        | UseCase内に注入 |
| API契約（抽象ポート）        | Adapter.Abstractions | `IExchange*Port`                  | 上位からは共通I/Fのみを呼ぶ |

---

## 1. HTTP 通信（Rest.Core）

### 1.1 I/F
```csharp
public interface IHttpClient {
    Task<HttpResponse> SendAsync(HttpRequest request, CancellationToken ct);
}
public readonly record struct HttpRequest(string Method, Uri Url, IReadOnlyDictionary<string,string> Headers, ReadOnlyMemory<byte> Body);
public readonly record struct HttpResponse(int Status, ReadOnlyMemory<byte> Body, IReadOnlyDictionary<string,string> Headers);
```

### 1.2 ポリシー
- **接続**: SocketsHttpHandler / 接続プール / DNS再解決有効
- **TLS**: OS 既定、自己署名は不可
- **圧縮**: `Accept-Encoding: gzip` 既定ON（Extensionで透過解凍）

---

## 2. JSON（Rest.Extension / IJson）

### 2.1 既定設定（System.Text.Json）
- `PropertyNamingPolicy = null`（取引所レスポンスに忠実）
- `DefaultIgnoreCondition = WhenWritingNull`
- `NumberHandling = AllowReadingFromString`
- `Converters`: DateTimeOffset(UnixMs/UnixSec両対応), Decimal(カルチャ固定), Enum(文字列)

```csharp
public interface IJson {
    T Deserialize<T>(ReadOnlySpan<byte> utf8);
    byte[] Serialize<T>(T value);
}
```

### 2.2 失敗時
- `Deserialization` を `ErrorCode` に正規化し、元JSONの先頭256Bを `Detail` に保存（PIIはマスク）。

---

## 3. リトライ／サーキット／タイムアウト（Rest.Extension）

### 3.1 方針
- **対象**: `GET` と **Idempotency-Key付き** `POST` のみ。
- **バックオフ**: 100ms 基点、指数2.0、ジッタ ±20%、最大 5 回。
- **中断**: `429, 5xx, Network, Timeout` のみ。`4xx（400/401/403/404）` は即失敗。
- **サーキット**: 連続失敗割合とP95遅延悪化で開放。半開は1本。

```csharp
public interface IRetryPolicy {
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
}
```

---

## 4. 構造化ログ／トレース（Rest.Extension）

### 4.1 ログフィールド
- `Timestamp, CorrelationId, RequestId, Exchange, Endpoint, HttpMethod, HttpStatus, DurationMs, RetryCount, ErrorCode`

### 4.2 マスキング規則
- `Authorization`, `ApiKey`, `Signature`, `Secret`, `Nonce` は全桁マスク。

### 4.3 トレース
- `Activity` を層ごとに開始し、親子関係を保持（W3C Trace Context）。

---

## 5. 署名／認証ヘッダ（Protocol）

```csharp
public interface IProtocolSigner {
    SignedRequest Sign(HttpRequestMessage req, ApiKey key, in Nonce nonce);
}
public readonly record struct Nonce(long Value);
public readonly record struct SignedRequest(HttpRequestMessage Request, string IdempotencyKey);
```
- **Nonce生成**: `IClock.UtcNow` と単調増加カウンタの合成で衝突回避。
- **Idempotency-Key**: 注文系POSTに必須。`{clientId}-{ticks}-{random}`。

---

## 6. 時刻同期（Protocol）

- **目的**: 署名検証失敗（`InvalidNonce`）やServer側の`recvWindow`違反を回避。
- **手順**:
  1) 起動時に `GET /time` を参照（各取引所のエンドポイントに依存）  
  2) `skew = server - IClock.UtcNow` を計測し、`MaxClockSkew` 内に収まるよう補正
  3) ドリフトは `NTP-like` に指数移動平均で平滑化
- **設定**: `ProtocolOptions.MaxClockSkew = 2s` 既定

---

## 7. レート制限（Protocol / IRateGate）

- **キー設計**: `"{Exchange}:{Endpoint}:{ApiKey}"`。
- **アルゴリズム**: トークンバケット（補充レートは取引所設定から導出）。
- **協調**: プロセス内の共有ストア（ConcurrentDictionary + Stopwatch）。
```csharp
public interface IRateGate { ValueTask WaitAsync(string key, CancellationToken ct); }
```

---

## 8. エラーマッピング（Adapter / IExchangeErrorMap）

- **目的**: 取引所固有のエラー（コード/メッセージ）を共通 `ErrorCode` に正規化。
- **規約**:
  - ネットワーク層: `Network/Timeout/RateLimited`
  - 署名/認証: `Unauthorized/InvalidNonce`
  - 業務系: `InsufficientFunds/InvalidRequest`
  - 解析失敗: `Deserialization`
- **例**
```csharp
public interface IExchangeErrorMap {
    Error Map(HttpResponse resp, ReadOnlySpan<byte> body);
}
```

---

## 9. DTO ⇔ Domain 変換（Application / IDtoMapper）

- **Mapperの原則**: 片方向変換を明示。丸め/桁数/通貨単位を Domain 型で検証。
- **ユースケース**: `PlaceMarketDto` → `PlaceMarket`、`Order` → `OrderResponseDto`。

```csharp
public interface IDtoMapper {
    PlaceMarket ToDomain(PlaceMarketDto dto);
    OrderResponseDto ToDto(Order order);
}
```

---

## 10. Idempotency（重複防止）

- **対象**: 発注/取消 POST。
- **Key生成**: Protocolで付与、Adapterはそのままヘッダに流す（取引所がサポートしない場合、自前で送信側デデュープ）。
- **再送規約**: `POST` は **成功レスポンス同一** を期待して再照会で整合確認。

---

## 11. ページング／リスト取得規約

- **共通パラメータ**: `limit`, `cursor`（タイムスタンプ/IDのどちらか）。
- **結果**: `Result<Page<T>>`（`Items`, `NextCursor`）。
```csharp
public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor);
```

---

## 12. 設定（Options）

| オプション                  | 既定値     | 説明 |
|-----------------------------|------------|------|
| `HttpOptions.Timeout`       | 5s         | 総合タイムアウト |
| `RetryOptions.MaxTry`       | 5          | 再試行最大回数 |
| `RetryOptions.BaseDelay`    | 100ms      | バックオフ基点 |
| `ProtocolOptions.MaxClockSkew` | 2s      | 許容時刻差 |
| `JsonOptions.AllowStringNumber` | true   | "123"→数値の許可 |
| `RateLimitOptions.FillRate` | per-API    | 取引所設定依存 |

---

## 13. 最小実装サンプル（擬似コード）

```csharp
public async Task<Result<Order>> PlaceMarketAsync(PlaceMarket cmd, CancellationToken ct) {
    await _rateGate.WaitAsync(Key("POST /order"), ct);
    var req = BuildRequest(cmd).WithIdempotency();
    var signed = _signer.Sign(req, _keys, _nonce.Next());
    return await _retry.ExecuteAsync<Result<Order>>(async ct2 => {
        var resp = await _http.SendAsync(signed.ToHttpRequest(), ct2);
        if (resp.Status >= 200 && resp.Status < 300) {
            var dto = _json.Deserialize<ExchangeOrderDto>(resp.Body.Span);
            var ord = _mapper.ToDomain(dto);
            return Result<Order>.Ok(ord);
        }
        return Result<Order>.Fail(_errors.Map(resp, resp.Body.Span));
    }, ct);
}
```

---

## 14. 責務マトリクス（Decision Matrix）

| 判断テーマ      | 置き場所 | 根拠 |
|-----------------|----------|------|
| 冪等制御        | Protocol | Key生成と再送安全判定は技術的関心 |
| 例外正規化      | Adapter  | 取引所知識が必要 |
| ログ相関ID      | Extension| 全層共通で透過させる |
| JSONポリシー    | Extension| 実装横断の一貫性 |
| 丸め/桁数検証   | Domain   | ビジネスルール |

---

## 15. テスト観点

- **契約テスト**: 各取引所レスポンス → 共通 `ErrorCode` / DTO のスナップショット一致。
- **耐障害**: 強制 `429` / `5xx` / ソケット切断でリトライ挙動を検証。
- **時刻差**: `±5s` のスキュー下で署名成功を確認。

---

## 16. 運用メトリクス（推奨）

- `api_requests_total{exchange,endpoint,method}`
- `api_request_duration_ms_bucket{...}`
- `api_retries_total{reason}`
- `api_rate_limit_wait_ms_sum`
- `api_errors_total{error_code}`

---

## 付録A) 例外とErrorCodeの対応（推奨表）

| 事象                           | ErrorCode            |
|--------------------------------|----------------------|
| TCP切断/名前解決失敗           | `Network`            |
| 応答遅延/Timeout               | `Timeout`            |
| 401/署名不一致/鍵失効          | `Unauthorized`       |
| 429（取引所レート制限）        | `RateLimited`        |
| 5xx（取引所障害）              | `ExchangeDown`       |
| JSON解析失敗                   | `Deserialization`    |
| パラメータ不正                 | `InvalidRequest`     |
| 資金不足                       | `InsufficientFunds`  |
| Nonce/Window不正               | `InvalidNonce`       |
| その他不明                     | `Unknown`            |


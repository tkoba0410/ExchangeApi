# 2025-08-27_DevLog_E2200_ContractsV2_MockMVP

- **DocType**: DevLog
- **DocNo**: E2200
- **Title**: Contracts v2 & Mock Exchange — MVP path established
- **Date**: 2025-08-27 (JST)
- **Status**: Draft → Done
- **Owner**: tkoba0410
- **Related**: E2200_ExchangeApi_VisualStudio_Structure_Proposal.md, A1000/A1100, D2100/D2200/D2300

---

## 1. Purpose / Scope（目的・範囲）
7層/DDD 方針に沿って、MVP の実行経路（Mock 取引所による Ticker/発注）を最小構成で通す。合わせて契約（Contract）レイヤを `Result<T>` で統一し、実アダプタ実装に進めるための基盤を固める。

---

## 2. Work Items（実施作業）
- **Contracts v2**: 値オブジェクト（`Symbol`, `ExchangeOrderId`, `ClientOrderId`）、`DateTimeOffset(UTC)`、`Result<T>` で統一
- **Core/Adapter/Foundation**: `Result<T>`, `Error`, `IHttpClient`, `ExchangeAdapterBase`, `DefaultHttpClient`
- **Mock Integration**: `MockExchange`（Ticker/板/OHLCV/発注/取消/照会/オープン一覧/残高）
- **Applications/CLI**: `ExchangeService`、CLI デモ（Result ハンドリング）
- **Bybit Spike**: 公開 Ticker（`/v5/market/tickers?category=spot&symbol=...`）
- **Docs**: `E2300_Next_Steps_Kickoff.md` 作成、DevLog 本書

---

## 3. Changes（差分）
| Area | Path | Type | Notes |
|---|---|---|---|
| **docs** | `docs/E2300_Next_Steps_Kickoff.md` | Add | 次段階キックオフ概要 |
| **AbstractExchange** | `Abstractions/Primitives.cs` | Add/Replace | ID型・enum・`Precision` |
|  | `Abstractions/MarketDataModels.cs` | Add/Replace | `Ticker`, `OrderBook`, `Ohlcv`, `OhlcvInterval` |
|  | `Abstractions/TradingModels.cs` | Add/Replace | `PlaceOrderRequest`, `OrderRef`, `OrderSnapshot`, `CancelOrderRequest`, `GetOrderRequest`, `Page<T>` |
|  | `Abstractions/BalanceModels.cs` | Add/Replace | `Balance`, `AccountInfo` |
|  | `Interfaces/IExchange.cs` | Replace | **`Result<T>` 戻り値へ統一** |
| **Rest.Core** | `Core/Result.cs`, `Core/Error.cs` | Add | 成功/失敗ハンドリング基盤 |
|  | `Http/IHttpClient.cs`, `Protocol/ISigner.cs` | Add | HTTP/署名抽象 |
| **Rest.Adapter** | `Adapters/ExchangeAdapterBase.cs` | Replace | `Result<T>` 版抽象 |
| **Foundation** | `Net/DefaultHttpClient.cs` | Add | `HttpClient` ラッパ |
| **Integrations.Mock** | `ExchangeApi.Integrations.Mock.csproj`, `MockExchange.cs` | Add | 最小実装 |
| **Applications** | `Services/ExchangeService.cs` | Replace | `Result<T>` 版委譲 |
| **CLI** | `samples/ExchangeApi.Cli/Program.cs` | Replace | デモ（Result表示） |
| **Integrations.Bybit** | `ExchangeApi.Integrations.Bybit.csproj`, `BybitExchange.cs` | Add/Fix | 公開 Ticker スパイク / XML ルート修正 |

---

## 4. Commands（実行コマンド）
```bash
# sln 登録（必要分）
dotnet sln ExchangeApi/ExchangeApi.sln add   ExchangeApi/src/ExchangeApi.Integrations.Mock/ExchangeApi.Integrations.Mock.csproj   ExchangeApi/src/ExchangeApi.Integrations.Bybit/ExchangeApi.Integrations.Bybit.csproj

# ビルド
dotnet build ExchangeApi/ExchangeApi.sln

# CLI（Mock）
dotnet run --project ExchangeApi/samples/ExchangeApi.Cli

# テスト（Mock）
dotnet test ExchangeApi/ExchangeApi.sln
```

---

## 5. Verification / Result（検証・結果）
- CLI 出力（例）  `[MockExchange] BTCJPY: bid=100 ask=101`  `Order <GUID>: status=Filled avgPrice=101 @ 2025-08-27 10:00:00Z`
- xUnit：Ticker/発注/照会/取消/オープン一覧/残高の基本フローが Green（※プロジェクト参照パス調整後）

---

## 6. Issues & Fixes（問題と対処）
- **CS0019**: `string` と `ExchangeOrderId` で `??` 使用 → `request.ExchangeOrderId ?? new ExchangeOrderId(...)` に修正
- **NU1104**: テスト `ProjectReference` 相対パス不足 → `..` を 1 つ増やす / 物理フォルダ移動で解決
- **MSB4025**: Bybit `.csproj` ルート要素重複 → 全置換で1つに統一

---

## 7. Next Actions（次アクション）
1. Bybit: **OrderBook or OHLCV** を追加（公開 API ベース）
2. 国内検証用に **bitFlyer BTC_JPY Ticker** スパイク
3. Rest.Core: **Retry/429** ポリシー雛形
4. xUnit 増強（失敗パス/エラーコード）
5. CLI：Mock/Bybit 切替フラグ対応

---

## 8. Notes（メモ）
- 時刻は **`DateTimeOffset (UTC)`** 統一。表示時に JST 変換。
- ID は **値オブジェクト** で取り違い防止（`ExchangeOrderId`, `ClientOrderId`）。
- ページングは `Page<T>`（シンプルカーソル）。実アダプタで拡張予定。

---

## 9. Commit Message（雛形）
```
feat: contracts v2 & MVP path via Mock; unify Result<T>
- AbstractExchange: ids, models, Result<T> interfaces
- Rest.Core/Adapter/Foundation: primitives & base classes
- Integrations.Mock: ticker/book/ohlcv + order/cancel/get/list/balances
- Applications/CLI: result-based demo
- Bybit: public ticker spike
- docs: E2300 next steps kickoff, devlog
```
# 🚀 Next Steps Kickoff (Aug 27, 2025)

このドキュメントは、次段階の実装に向けた **最小実装 (MVP)** を示します。  
本コミットでは以下を追加しました。

## 1. 抽象レイヤ（AbstractExchange）
- `IExchange` / `IMarketDataApi` / `ITradingApi` / `IAccountApi`
- 取引モデル: `PlaceOrderRequest`, `OrderSnapshot`, 各種 enum/record
- マーケットデータ: `Ticker`, `OrderBook`, `Ohlcv`

## 2. REST.Core
- `Result<T>`, `Error`
- `IHttpClient`, `ISigner` のプロトコル下位抽象

## 3. REST.Adapter
- `ExchangeAdapterBase` を追加（各取引所アダプタの基底クラス）

## 4. Foundation
- `DefaultHttpClient` 実装（`HttpClient` ラッパ）

## 5. Integrations.Mock（新規プロジェクト）
- メモリ内で動く `MockExchange` を追加（価格100/101）
- PlaceOrderは即時Filledでスナップショット返却

## 6. CLI
- `ExchangeApi.Cli` から `MockExchange` を呼ぶサンプルを追加
  - Ticker取得
  - Market注文（即時約定）

---

## 🧩 ビルド & 実行

1. **ソリューションにプロジェクトを追加**
   - `src/ExchangeApi.Integrations.Mock/ExchangeApi.Integrations.Mock.csproj` をソリューションに追加
2. **参照確認**
   - `ExchangeApi.Cli` は `Applications`, `Foundation`, `Integrations.Mock` を参照
3. **実行**
   ```bash
   dotnet build
   dotnet run --project samples/ExchangeApi.Cli
   ```

---

## ✅ Done Criteria（今回）
- I/Fがドキュメントの粒度と整合
- CLIでTicker/発注の最小動作を確認可能

## ▶️ 次にやること（提案）
1. **Result<T>の全面適用**  
   - `PlaceOrderAsync` 等を `Result<OrderSnapshot>` に切り替え（親仕様A1000の「Result<T>で返る」に合わせる）
2. **DTO ↔ Domain変換の方針**  
   - `Applications` に `Mapping` 置き場を用意し、**ドメインモデルは純粋**を維持
3. **Protocol層の実装開始**  
   - `ISigner` 実装（HMAC-SHA256）、時刻同期、nonce生成ユーティリティ
4. **Rest.Core: Retry/RateLimit**  
   - ポリシー（指数バックオフ/429ハンドリング）
5. **Adapter: Bybit/bitFlyerの雛形**  
   - `GetTicker`/`PlaceOrder` の1~2 APIエンドポイントのみ先行実装
6. **テスト**  
   - `xunit` で `MockExchange` を使ったユニットテスト起票

> 変更履歴: 2025-08-27 初版

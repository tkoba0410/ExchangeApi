
# ✅ Done Criteria（詳細版）— Exchange API Library

> 本章は D002「親仕様書（詳細版）」の **6. Done Criteria** を、**測定可能・自動判定可能**な基準として定義する。  
> 対象は機能・品質（信頼性/性能/可観測性/セキュリティ）・テスト・ドキュメント・CI/CD・互換性・リリースの各観点。

---

## 0. 判定原則

- **数値化**：閾値は P50/P95/P99 等の分位で定義し、CI 内パフォーマンステストで自動集計。
- **自動化**：可能な限り **CI ジョブで自動合否**。人手判定は最小化（レビュー系のみ）。
- **スコープ**：Core/Extension/Protocol/Adapter.Abstractions/Exchange.*/Application/Api.Public を含む。

---

## 1. 機能完了基準（Functional）

| 項目 | 基準 |
|---|---|
| 発注（成行/指値） | `ITradingApi.PlaceMarket/PlaceLimit` が **Result<T>** で成功・エラーを返し、Idempotency-Key を送出すること |
| 注文照会/キャンセル | 注文ID/クライアントIDの両方で照会・キャンセル可能 |
| 残高取得 | 口座単位で正確な通貨桁数/丸めで返却 |
| エラーマップ | 主要 10 事象（Network/Timeout/Unauthorized/RateLimited/ExchangeDown/InvalidNonce/InvalidRequest/InsufficientFunds/Deserialization/Unknown）を網羅 |
| ページング | `Page<T>`（Items/NextCursor）で取得可能 |
| 時刻同期 | Protocol による `MaxClockSkew` 内への収束（±2s 既定） |

---

## 2. 性能基準（Performance）

**テスト条件**：有線ネットワーク、CPU 4C/8GB、.NET Release、100 同時実行（擬似エンドポイント）。

| 指標 | 目標値 |
|---|---|
| Application ユースケース遅延 P95 | ≤ 50 ms（取引所応答時間を除く） |
| JSON（逆/直列化）1 回 | ≤ 5 ms / ≤ 5 ms |
| HTTP 送出オーバーヘッド P95 | ≤ 10 ms（Rest.Core 単体） |
| スループット | 1K req/s（ローカル擬似サーバ、Keep-Alive） |
| メモリ一時確保 | 1 リクエストあたり ≤ 64 KB（GC Gen0 抑制） |

---

## 3. 信頼性・回復性（Resilience）

| 項目 | 基準 |
|---|---|
| リトライ成功率 | `429/5xx/Network/Timeout` を対象に **再送後の成功率 ≥ 95%**（擬似障害混入試験） |
| サーキットブレーカ | 連続失敗で開、半開 1 本、回復後自動閉。誤発火率 1% 未満 |
| 冪等性 | 同一 Idempotency-Key の重複送信で **副作用が重複しない**（E2E 検証） |
| 時刻スキュー | ±5s のドリフト下でも署名検証を通過 |
| レート制限 | `IRateGate` 準拠でスパイク時の**ドロップゼロ**（待機により調整） |

---

## 4. セキュリティ（Security）

| 項目 | 基準 |
|---|---|
| 秘密情報 | API キーは `ISecretProvider` 経由で取得。**平文保存禁止** |
| ログ | `Authorization/ApiKey/Signature/Secret/Nonce` を全桁マスク |
| 依存脆弱性 | SCA（OSS Index 等）で **High/CRITICAL 0 件** |
| SAST | 静的解析で危険 API 使用 0 件、Dispose 漏れ 0 件 |
| 署名 | 既知ベクトルテストの全合格（Binance/Bybit 等の HMAC/EdDSA） |

---

## 5. 可観測性（Observability）

| 項目 | 基準 |
|---|---|
| 構造化ログ | 必須フィールド（CorrelationId/RequestId/Exchange/Endpoint/Status/DurationMs/ErrorCode/RetryCount）を出力 |
| メトリクス | `api_requests_total`, `api_request_duration_ms`, `api_retries_total`, `api_errors_total`, `api_rate_limit_wait_ms` を出力 |
| トレース | 全層で `Activity` を連結。1 リクエストにつき 1 本以上のスパンが生成 |

---

## 6. テスト合格基準

| テスト種別 | 合格基準 |
|---|---|
| 単体（Unit） | 全プロジェクト合計 **成功率 100%**、カバレッジ **80% 以上** |
| 契約（Contract） | 取引所レスポンス → 共通 DTO/エラーマップの **スナップショット一致** |
| 結合（Integration） | サンドボックス鍵で発注/照会/取消/残高を **実通信で成功** |
| 耐障害（Chaos） | 10% `5xx` / 10% `429` / 5% Network Drop 混入でユースケース成功率 **≥ 99%** |
| 回帰（Regression） | 主要ユースケースのゴールデンパスが**差分ゼロ** |
| 負荷（Load） | 10 分間、SLA 達成（性能指標参照） |

---

## 7. ドキュメント完了基準

| 項目 | 基準 |
|---|---|
| 公開 API | XML ドキュメントコメント（C#）を 100% 付与、ビルド時警告 0 |
| README | 各パッケージに導入/使用例/設定/サンプルコードを記載 |
| 仕様書 | D001（Simple）/D002（Detailed）と各詳細章（Architecture/Module/Common/Steps/Done）を**最新版反映** |
| 変更履歴 | CHANGELOG に SemVer 準拠で差分明記 |

---

## 8. CI/CD 基準

| 項目 | 基準 |
|---|---|
| ビルド | Release ビルド成功。ワーニングは **0** |
| 静的解析 | Style/Analyzers で規約違反 **0**（ルールセット同梱） |
| 単体/契約/負荷 | CI で自動実行。閾値未達は **失敗** とする |
| パッケージ | NuGet 作成、サイズ ≤ 5 MB/個、不要依存 0 |
| 署名 | アセンブリ署名（Strong Name）と SBOM 生成 |

---

## 9. 互換性・破壊変更ポリシー

| 範囲 | 基準 |
|---|---|
| `Api.Public` / `Adapter.Abstractions` | **Minor** では後方互換必須（破壊変更は Major のみ） |
| `Exchange.*` | Minor で機能追加可。破壊変更は Major |
| シリアライズ | DTO の互換は Minor で維持（新規フィールドは nullable/既定値） |

---

## 10. Adapter 実装合格基準（各取引所プラグイン）

| 項目 | 基準 |
|---|---|
| 機能 | Trading/Account/MarketData 各 *Port* を満たす |
| エラーマップ | 公式ドキュメント記載コードの **90% 以上** を網羅 |
| 既知ベクトル | 署名・タイムスタンプ・時刻同期のベクトルテスト全合格 |
| レート制限 | ドキュメント値に基づき `IRateGate` を設定。超過時は自動待機 |
| ドキュメント | README に対応エンドポイント表・既知制約・例外対応を記載 |

---

## 11. リリース Go/No-Go チェックリスト

- [ ] すべての CI ジョブが **Success**（ユニット/契約/負荷/静的解析/パッケージ）  
- [ ] 性能測定値が **全閾値を満たす**（P95 遅延、スループット、メモリ）  
- [ ] セキュリティ診断で **High/CRITICAL 0 件**  
- [ ] サンドボックス E2E で **全ユースケース成功**  
- [ ] ドキュメント一式更新（README/CHANGELOG/仕様書）  
- [ ] バージョン・タグ・SBOM・署名が発行済み  

---

## 12. 付録：測定シナリオ（サンプル）

- **Load/Latency**：擬似エンドポイント（固定 200/429/5xx 比率）に 100 並列で注文/照会/取消を 10 分間送出。  
- **Chaos**：ネットワーク Drop/遅延注入（50–300ms）を 20% の確率で発生。  
- **Idempotency**：同キーで 3 回連続 POST、応答と最終状態が 1 回分に収束することを照会で確認。


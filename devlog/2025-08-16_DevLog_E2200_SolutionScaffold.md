# 開発記録（2025-08-16）

## 今日行った作業
1. `.NET SDK` バージョン確認（9.0.304）
2. SDK 固定の必要性を検討 → 個人開発のため固定せず進める方針決定
3. E2200 提案書に基づく Visual Studio ソリューション構造の作成手順を整理
4. リポジトリルートでソリューション作成
5. `src/`, `tests/`, `samples/`, `build/`, `tools/` ディレクトリ作成
6. 各レイヤーのクラスライブラリプロジェクト作成（Foundation, Rest.Core, Rest.Adapter, AbstractExchange, Domain, Integrations, Applications）
7. 各レイヤーのテストプロジェクト作成（xUnit）
8. CLI サンプルプロジェクト作成
9. プロジェクト間参照追加（DDD依存方向遵守）
10. ソリューションに全プロジェクト追加
11. `build/Directory.Build.props` と `.editorconfig` 作成
12. `dotnet build` にてビルド確認
13. Git ステージング・コミット・GitHub へプッシュ

## 次回開始のためのポイント
- `src/ExchangeApi.Foundation` に Result 型・Guard クラスなどの横断ユーティリティを実装する
- `src/ExchangeApi.AbstractExchange` に `IOrderPort`, `IAccountPort`, `IMarketDataPort` インターフェースの雛形追加
- CLI プロジェクトで簡易なエントリーポイントを作り、アプリケーション層を呼び出せる形にする
- 単体テストプロジェクトに最初のテストケースを追加し、`dotnet test` の動作を確認

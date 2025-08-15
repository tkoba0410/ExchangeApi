# E2200\_ExchangeApi Visual Studio 構成提案書.md

- **分類**: 追加文書
- **作成日**: 2025-08-15
- **版数**: 1.0

## 目的

本書は、ExchangeApiプロジェクトをVisual Studioおよび`.NET 8`環境で実装するための推奨構成を示すものである。 DDD（ドメイン駆動設計）、責務分離、テスト容易性、拡張性、ドキュメント整備といった方針を満たすスケルトン構造を提供する。

## 構成概要

（レイヤー概要、ディレクトリ構成、プロジェクト間依存関係などを示す）

# ExchangeApi Visual Studio 構成提案（2025-08-15）

この構成は、DDD・責務分離・拡張性・テスト容易性・ドキュメント整備という方針と、 「7層構造」の意図を両立するための **.NET 8 (LTS)** 前提の Visual Studio / `dotnet` CLI 向け提案です。

```
ExchangeApi/
├─ src/
│  ├─ ExchangeApi.Foundation/                # 共有ユーティリティ/プリミティブ、Result型、Guard、Clock、Json、Retry等
│  ├─ ExchangeApi.Rest.Core/                 # REST共通：HTTPクライアント、署名、レート制限、リトライ、シリアライズ
│  ├─ ExchangeApi.Rest.Adapter/              # RESTアダプタ：エンドポイント定義、リクエスト/レスポンスDTO、エラー変換
│  ├─ ExchangeApi.AbstractExchange/          # 抽象ポート：取引所に依存しないUseCase向けインタフェース（OrderPort等）
│  ├─ ExchangeApi.Domain/                    # ドメイン（エンティティ/VO/集約/ドメインサービス、RecoveryMode等の戦略）
│  ├─ ExchangeApi.Integrations/
│  │  ├─ ExchangeApi.Integrations.Binance/   # 具体取引所実装（Binance）
│  │  ├─ ExchangeApi.Integrations.Bybit/     # 具体取引所実装（Bybit）
│  │  └─ ExchangeApi.Integrations.* /        # その他取引所の実装を追加可能
│  └─ ExchangeApi.Applications/              # アプリケーションサービス（ユースケース、CQRS的オーケストレーション）
│
├─ tests/
│  ├─ ExchangeApi.Foundation.Tests/
│  ├─ ExchangeApi.Rest.Core.Tests/
│  ├─ ExchangeApi.Rest.Adapter.Tests/
│  ├─ ExchangeApi.AbstractExchange.Tests/
│  ├─ ExchangeApi.Domain.Tests/
│  ├─ ExchangeApi.Integrations.Binance.Tests/
│  ├─ ExchangeApi.Integrations.Bybit.Tests/
│  └─ ExchangeApi.Applications.Tests/
│
├─ samples/
│  ├─ ExchangeApi.Cli/                       # CLIサンプル（発注/資産照会/板取得など）
│  └─ ExchangeApi.DemoWeb/                   # 最小API or Razor Pages サンプル（任意）
│
├─ docs/                                     # いただいた文書（D000〜D024等）をここに配置
│  └─ (imported from: exchange_api_docs_latest.zip)
│
├─ build/
│  ├─ Directory.Build.props                  # LangVersion/Nullable/Analyzers/StyleCop等の共通設定
│  ├─ Directory.Build.targets                 # 共通ビルド後処理（ソースリンタ/パッケージング）
│  └─ ruleset/                               # StyleCop/EditorConfig補完（必要に応じて）
│
├─ tools/
│  └─ scripts/                               # CI/CDやローカル補助スクリプト
│
├─ ExchangeApi.sln
├─ .editorconfig
├─ .gitignore
└─ README.md
```

## レイヤーと責務のマッピング

- **Foundation**: 横断関心事（結果型、時計、ID生成、JSON、Polly的リトライ、レート制限トークンバケット等）。
- **Rest.Core**: HTTP/RESTの基盤。署名器・タイムシンク・バックオフ・`HttpClientFactory`利用。
- **Rest.Adapter**: 取引所REST仕様に対するアダプタ。DTO/エンドポイント/例外→ドメインエラー変換。
- **AbstractExchange**: アプリケーションが依存するポート（`IOrderPort`, `IAccountPort`, `IMarketDataPort`等）。
- **Domain**: 注文・ポジション・約定・回復戦略（`RecoveryMode`：LossCutReentry/ReverseReentry/CustomSpreadRecovery/ LossCutRebalance 等）をエンティティ/VOで表現。
- **Integrations**: 具体取引所実装（Binance/Bybit/...）。`AbstractExchange`準拠で置換可能。
- **Applications**: ユースケース層。ドメイン/ポートをオーケストレートし、トランザクション境界や入力検証を担当。

## docs の取り込み元（アップロードZipの展開ツリー）

```
exchange_api_docs/
  A1000_MasterSpec_Simple.md
  A1100_MasterSpec_Detailed.md
  D2100_Architecture_7Layers_Detailed.md
  D2200_Module_Composition_Detailed.md
  D2300_Common_Crosscutting_Detailed.md
  E2000_Implementation_Steps_Detailed.md
  E2100_Done_Criteria_Detailed.md
  R0000_Document_Management_Policy.md
  exchange_api_docs_latest.zip
  index.md
```

## Visual Studio での作成手順（`dotnet` CLI）

1. ルート作成: `mkdir ExchangeApi && cd ExchangeApi`
2. ソリューション: `dotnet new sln -n ExchangeApi`
3. プロジェクト作成（抜粋、全体はスクリプト参照）:
   - `dotnet new classlib -n ExchangeApi.Foundation -o src/ExchangeApi.Foundation`
   - `dotnet new classlib -n ExchangeApi.Rest.Core -o src/ExchangeApi.Rest.Core`
   - `dotnet new classlib -n ExchangeApi.Rest.Adapter -o src/ExchangeApi.Rest.Adapter`
   - `dotnet new classlib -n ExchangeApi.AbstractExchange -o src/ExchangeApi.AbstractExchange`
   - `dotnet new classlib -n ExchangeApi.Domain -o src/ExchangeApi.Domain`
   - `dotnet new classlib -n ExchangeApi.Integrations.Binance -o src/ExchangeApi.Integrations/ExchangeApi.Integrations.Binance`
   - `dotnet new classlib -n ExchangeApi.Integrations.Bybit -o src/ExchangeApi.Integrations/ExchangeApi.Integrations.Bybit`
   - `dotnet new classlib -n ExchangeApi.Applications -o src/ExchangeApi.Applications`
   - `dotnet new xunit -n ExchangeApi.Domain.Tests -o tests/ExchangeApi.Domain.Tests`
   - `dotnet new console -n ExchangeApi.Cli -o samples/ExchangeApi.Cli`
4. 参照関係（例）:
   - `dotnet add src/ExchangeApi.Rest.Adapter/ExchangeApi.Rest.Adapter.csproj reference src/ExchangeApi.Rest.Core/ExchangeApi.Rest.Core.csproj`
   - `dotnet add src/ExchangeApi.AbstractExchange/ExchangeApi.AbstractExchange.csproj reference src/ExchangeApi.Rest.Adapter/ExchangeApi.Rest.Adapter.csproj`
   - `dotnet add src/ExchangeApi.Domain/ExchangeApi.Domain.csproj reference src/ExchangeApi.AbstractExchange/ExchangeApi.AbstractExchange.csproj`
   - `dotnet add src/ExchangeApi.Applications/ExchangeApi.Applications.csproj reference src/ExchangeApi.Domain/ExchangeApi.Domain.csproj`
   - `dotnet add src/ExchangeApi.Integrations.Binance/ExchangeApi.Integrations.Binance.csproj reference src/ExchangeApi.Rest.Adapter/ExchangeApi.Rest.Adapter.csproj`
   - `dotnet add samples/ExchangeApi.Cli/ExchangeApi.Cli.csproj reference src/ExchangeApi.Applications/ExchangeApi.Applications.csproj`
5. ソリューションへ追加: `dotnet sln add **/*.csproj`

## 共通ビルド設定（`build/Directory.Build.props` 概要）

- `<TargetFramework>net8.0</TargetFramework>`
- `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`
- Analyzers（`Microsoft.CodeAnalysis.NetAnalyzers`）、StyleCop（任意）
- `TreatWarningsAsErrors` は CI で有効化推奨
- `InternalsVisibleTo` は Tests に限定

## 留意事項

- プロジェクト参照関係はDDDの依存方向を厳守する。
- CI/CD（GitHub Actions等）に組み込みやすいように、共通ビルド設定を`build/Directory.Build.props`で管理。
- docs配下に本書を含む全設計文書を配置し、バージョン管理に含める。


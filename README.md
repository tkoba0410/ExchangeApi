# ExchangeApi

統一インターフェースで複数の仮想通貨取引所 REST API を扱うための C# ライブラリ。

## 目的
- 取引所ごとの差異を抽象化し、共通のドメインモデルで扱えるようにする
- 責務分離とテスト容易性を重視（アダプタ差し替え前提）
- 拡張可能なレイヤ構成とドキュメント整備

## フォルダ／レイヤ概要（例）
- `src/` … ライブラリ本体
- `tests/` … ユニットテスト
- `docs/` … ドキュメント（設計・運用）
  - [`roles_and_workflow.md`](docs/roles_and_workflow.md) — 役割分担とワークフロー
  - （今後）`architecture.md`, `design.md` など

## はじめ方
```bash
# クローン
git clone <your-repo-url>
cd ExchangeApi

# ソリューションを Visual Studio で開く
# 必要に応じて .NET SDK をインストール

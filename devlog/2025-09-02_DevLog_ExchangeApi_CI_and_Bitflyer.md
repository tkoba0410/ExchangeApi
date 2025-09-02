# DevLog — ExchangeApi CI & Bitflyer Kickoff
**Date**: 2025-09-02 19:00 UTC+09:00 (Asia/Tokyo)

## TL;DR
- `docs/roles_and_workflow.md` を **独立文書**として採用、README からリンクする方針を確定。
- Visual Studio（VS）の **フォルダービュー/ソリューションビュー**の使い分けを整理。Solution Items に **README + docs 全部**を表示する運用に決定。
- **PR 実践**：ブランチ→コミット→プッシュ→PR→レビュー→マージの一連を確認。マージ方式（Squash/Merge/Rebase）の違いと、**学習目的なら「Merge pull request」推奨**で合意。
- **CI（GitHub Actions）**の位置と実態、`.github/workflows/*.yml` の基本を理解。最初は **build のみ**で導入、その後 `test` を段階追加する方針。
- **.NET 9 対応**の手順を確立（`<TargetFramework>net9.0</TargetFramework>`／`global.json`／CI の `setup-dotnet: 9.0.x`）。
- **bitFlyer 統合の着手**：VS プロジェクト雛形と **git patch** を作成（`src/ExchangeApi.Integrations.Bitflyer`）。
- **Codex の使い分け**：GPT＝仕様/文章化、Codex＝リポジトリ操作（PR出力）という分担を再確認。Codex には **短く・具体的・受け入れ条件つき**で依頼。

---

## 1) ドキュメント統合（roles_and_workflow）
- 配置: `docs/roles_and_workflow.md`（独立文書）。
- README へのリンクを追加（「ドキュメント」節）。
- VS ソリューションビューに **Solution Items** を作成し、`README.md` と `docs/*.md` をまとめて見せる方法を確立。
- 備考: ソリューションビューに「リンクとして追加」UIが見当たらない件は **Solution Items への追加**で解決。

### README 更新（置換用）
- 目的／レイヤ概要／ドキュメントリンクを追記（roles_and_workflow へのリンク含む）。

---

## 2) PR 実践と運用ルール
- VS からのブランチ作成／コミット／プッシュ／PR 作成をガイド。
- **マージ方式**の理解：
  - **Squash**…PR=1コミットで履歴が綺麗。
  - **Merge pull request**…学習向き。細かいコミットも残す。
  - **Rebase**…直線履歴だが上級者向け。
- 初心者が勉強重視の場合は **Merge pull request** を推奨。

---

## 3) CI（GitHub Actions）導入
- 実態: GitHub の ephemeral VM 上で `.yml` に沿って実行。
- 配置: `.github/workflows/build.yml`。最小形は **build のみ**（あとから test を追加）。

```yaml
name: .NET Build (minimal)
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'   # 9 系で固定
      - run: dotnet build --configuration Release
```

- 失敗が多い場合の対処：まず **build だけ** → 成功後に `dotnet test` を段階追加。

---

## 4) .NET 9 への更新方針
- 各 `.csproj`:
  ```xml
  <TargetFramework>net9.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  ```
- `global.json`:
  ```json
  {
    "sdk": {
      "version": "9.0.100",
      "rollForward": "latestFeature"
    }
  }
  ```
- CI: `actions/setup-dotnet@v3` の `dotnet-version: '9.0.x'`。

---

## 5) bitFlyer 統合のキックオフ
- 新規プロジェクト: `src/ExchangeApi.Integrations.Bitflyer`（net9.0）。
- 最小構成（`HttpClient` で公開APIを叩ける骨格）。
- 生成済み **パッチ**: `bitflyer_add_net9.patch`（GetTickerRawAsync 付き）  
  適用例:
  ```bash
  git switch -c feat/add-bitflyer
  git apply --check ./bitflyer_add_net9.patch
  git apply ./bitflyer_add_net9.patch
  dotnet sln ExchangeApi.sln add src/ExchangeApi.Integrations.Bitflyer/ExchangeApi.Integrations.Bitflyer.csproj
  dotnet build -c Release
  ```

---

## 6) Codex の使い方（今日の整理）
- **指示は短く・具体的・受け入れ条件つき**（1PR=1目的、30分以内目安）。
- 例（最小CI 依頼 5行テンプレ）:
  ```
  タスク: .NETの最小CI（buildのみ）を追加してPR
  ブランチ: ci/add-minimal-dotnet-build
  変更ファイル: .github/workflows/build.yml
  内容: checkout→setup-dotnet 9.0→dotnet build
  受け入れ条件: Actionsが起動しbuild成功、Files changedにbuild.ymlのみ
  ```

---

## 7) Git（差分とパッチ）メモ
- `git diff`：2つの状態の差分を表示（`--staged`, `main..feature` など）。
- `git patch`：差分をファイル化して配布・適用（`git apply`, `git am`）。
- GitHub の PR/コミットにも `.patch` URL があるため、`curl | git am -3` で直接適用可能。

---

## 8) コミットメッセージ例（フォルダ再編）
- `chore(repo): フォルダ構成を再編（src/tests/docs へ集約）`
- `refactor(structure): ディレクトリを再配置（動作変更なし）`
- `.sln/.csproj` のパス更新時は `build(sln)` / `build(csproj)` タグを併用。

---

## 9) 次のアクション（提案）
- [ ] `build.yml` を PR で追加（まずは build のみ／9.0.x）。
- [ ] `global.json` を 9 系で追加 or 更新。
- [ ] bitFlyer パッチ適用 → `.sln` に追加 → ビルド確認 → PR。
- [ ] `docs-link-check`（lychee）を別PRで導入、壊れリンク修正を自動化。
- [ ] 公開APIの DTO 実装（`getmarkets`/`getticker`）＋ 単体テストの追加。
- [ ] マージ方式は当面 **Merge pull request**、慣れたら **Squash** へ移行検討。

---

## 補足（参考スニペット）
**`dotnet test` を CI に追加する時の追記**：
```yaml
- name: Test
  run: dotnet test --no-build --verbosity normal
```

**PR 説明テンプレ**：
```
## What
- Add ExchangeApi.Integrations.Bitflyer (net9.0) minimal project
- Introduce minimal CI for build

## Why
- Kick off bitFlyer integration
- Ensure build health in CI

## Checklist
- [x] CI green
- [x] Only intended files changed
```

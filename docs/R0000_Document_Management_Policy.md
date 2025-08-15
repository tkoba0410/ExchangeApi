# R0001 — Document Management Policy

## 1. 目的
本方針は、Exchange API Library における開発文書を体系的に分類・管理するための番号体系および運用ルールを定義する。  
文書番号は恒久的かつ変わらない分類のみを含め、改訂や運用の柔軟性を保ちながら全仕様書を一貫性を持って管理する。

---

## 2. 番号構成原則
- **形式**
  ```
  <系統コード><上2桁カテゴリ番号><下2桁連番>
  ```
  - 系統コード：文書の性格（A, B, D, E, T, R, O）
  - 上2桁カテゴリ番号：系統内の章分類
  - 下2桁連番：カテゴリ内での文書番号（00は基幹文書）

- **ISO/JIS準拠**：分類コード（系統）を先頭に置き、「分類＋番号」の順序とする  
- 文書番号には**恒久的な分類（系統・カテゴリ・連番）のみを含める**  
  - バージョン、機密区分、状態、対象など変動しやすい要素は番号に含めず、別途メタ情報で管理

---

## 3. 系統コード一覧（想定）
| コード | 名称 | 主な内容 |
|--------|------|----------|
| **A** | Master（全体方針） | プロジェクト全体の目的・背景・構造概要 |
| **B** | Business / Requirements | ビジネス要件、機能・非機能要件、ユースケース |
| **D** | Design（詳細設計） | 技術構造、モジュール構成、I/F設計、データモデル |
| **E** | Execution（工程） | 実装ステップ、Done基準、開発計画、展開手順 |
| **T** | Testing / QA | テスト計画、テスト仕様、QAレポート、品質ゲート |
| **R** | Rules / Reference | 文書管理ルール、標準ガイドライン、用語集 |
| **O** | Operations | 運用マニュアル、監視、障害対応手順 |

---

## 4. カテゴリ番号一覧（想定）
| 系統 | 上2桁 | カテゴリ名 | 主な内容 |
|------|-------|------------|----------|
| A | 10 | Master Spec Simple | 概要版マスタードキュメント |
| A | 11 | Master Spec Detailed | 詳細版マスタードキュメント |
| B | 10 | Business Requirements | ビジネス要件 |
| B | 11 | Functional Requirements | 機能要件・ユースケース |
| B | 12 | Non-functional Requirements | 性能・可用性・セキュリティ要件 |
| D | 21 | Architecture | 層構造、依存規約、I/F概要 |
| D | 22 | Module Composition | パッケージ構成、依存関係 |
| D | 23 | Common Crosscutting | 横断的関心事（ログ、リトライ等） |
| D | 24 | Data Model | エンティティ、VO、DTO、DBスキーマ設計 |
| E | 20 | Implementation Steps | 実装手順 |
| E | 21 | Done Criteria | 完了基準 |
| E | 22 | Deployment Steps | デプロイ・展開手順 |
| T | 10 | Test Plan | テスト計画 |
| T | 11 | Test Specification | テストケース仕様 |
| T | 12 | QA Report | 試験結果報告 |
| R | 00 | Document Management | 文書番号体系、改訂ポリシー |
| R | 01 | Style Guide | 記法、用語統一 |
| R | 02 | Versioning Policy | バージョニング・改訂履歴ルール |
| O | 10 | Operations Manual | 運用手順 |
| O | 11 | Incident Response | 障害対応手順 |
| O | 12 | Monitoring Guide | 監視・アラート設定 |

---

## 5. 番号運用ルール
1. **新規発行**
   - 系統コード → 上2桁カテゴリ番号 → 下2桁連番の順で決定
   - 00はカテゴリの基幹文書として予約
2. **補足・派生文書**
   - 同カテゴリ内の次番号（01, 02, …）として発行
   - 親子関係はIndex.mdや管理台帳に記録
3. **破壊的変更**
   - 上位カテゴリ変更または系統変更を伴う場合は新番号を発行
4. **軽微変更**
   - 番号は据え置き、版数（v1.1等）で管理

---

## 6. メタ情報管理（番号に含めない分類）
- **バージョン**：改訂履歴管理用  
- **機密区分**：Public / Internal / Confidential など  
- **状態**：Draft / Review / Approved / Deprecated  
- **対象読者**：Developer / QA / Operator / Manager など  

これらは番号ではなく、Index.mdまたは文書ヘッダーで管理する。

---

## 7. 参考文献 / Reference
- ISO 9001:2015 — Quality Management Systems — Requirements  
- ISO 10013:2001 — Guidelines for Quality Management System Documentation  
- ISO/IEC/IEEE 15289:2019 — Systems and software engineering — Content of life-cycle information items  
- JIS Q 9001 — 品質マネジメントシステム — 要求事項  
- IEEE Std 1063-2001 — Standard for Software User Documentation  
- PMBOK® Guide — Project Management Institute

# 📘 Exchange APIライブラリ 親仕様書（Master Spec）

唯一の親仕様書。全体構造と層別方針を記載。

## 1. 目的
複数取引所の共通操作を統一インターフェースで抽象化。  
DDD準拠、Adapter設計、Result<T>利用。

## 2. アーキテクチャ（7層）

[ UI / API ]  
 ↓  
[ Application ]  
 ↓  
[ Domain.Model / Dto ]  
 ↓  
[ Adapter.XYZ ]  
 ↓  
[ Protocol ]  
 ↓  
[ Rest.Extension ]  
 ↓  
[ Rest.Core ]  

## 3. モジュール構成
- **Application**：UseCase, Mapper, Result
- **Domain**：Entity, VO, DTO
- **Adapter**：取引所実装
- **Protocol**：署名/ヘッダ
- **Rest.Core**：HTTP
- **Rest.Extension**：ログ/リトライ

## 4. 共通処理配置
- HTTP：Rest.Core
- ログ/リトライ/JSON：Rest.Extension
- DTO変換：Domain.Dto + Mapper
- 署名/ヘッダ：Protocol
- API契約：Abstract

## 5. 実装ステップ
1. α：最小動作（Rest + Domain + Application）
2. β：Adapter + Protocol
3. γ：拡張（Logger等）
4. RC：ドキュメント・テスト

## 6. Done Criteria
- PlaceMarketOrderがResult<T>で返る
- テスト通過 & CI組込

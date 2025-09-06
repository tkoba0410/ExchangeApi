# M2000 Migration Guide: Old Structure → v2.0

## 目的
- 旧構成から D2200 v2.0（7層 + Abstractions）への移行手順を示す。

## 読者別タスク
- 設計: 用語統一（AbstractExchange → Application.Ports）、DTO/Domain/Wire 境界の点検
- 文書: index導線更新、E2200の Superseded 化、相互リンク整備
- CI: G1〜G4 ガード導入（参照方向/禁輸型/純度/Wire漏れ）

## 語彙マップ
- AbstractExchange → Application.Ports
- Foundation → Rest.Extension / Rest.Core / Abstractions

## CI ガード導入
- 参照方向検査（csproj参照の逆流禁止）
- Abstractions に PackageReference 禁止
- Wire DTO の外部流出禁止（名前空間検査）

## 参考
- D2200_Module_Composition_Detailed.md v2.0（正典）
- D2100 Architecture（7層）

# University Management System - アーキテクチャコンテキスト

## システム概要

大学の履修管理・出席管理・成績評価を統合管理するシステム。
C# (.NET 9) + Entity Framework Core + DDD + レイヤーアーキテクチャ + CQRS パターンを採用。

## プロジェクト構造

```
UniversityManagement/
├── src/
│   ├── Shared/                         # 共有カーネル（StudentId等）
│   ├── StudentRegistrations/           # 学生在籍管理コンテキスト
│   │   ├── Domain/StudentAggregate/
│   │   ├── Application/Commands|Queries/
│   │   └── Infrastructure/Persistence/
│   ├── Enrollments/                    # 履修登録管理コンテキスト
│   │   ├── Domain/{Course|Semester|CourseOffering|Enrollment}Aggregate/
│   │   ├── Application/Commands|Queries|Services/
│   │   └── Infrastructure/Persistence/
│   ├── Api/                            # 統合API
│   ├── Attendances/                    # 出席管理（未実装）
│   └── Grading/                        # 成績評価（未実装）
├── tests/
├── contexts/CONTEXT_MAP.md             # コンテキストマップ
└── .claude/skills/                     # 実装パターンSkills
    ├── cqrs-ddd/                       # CQRS+DDD実装パターン
    ├── testing-strategy/               # テスト戦略
    └── slow-query-detector/            # N+1検出
```

## Skills（実装ガイド）

| Skill | 内容 |
| ----- | ---- |
| 📐 [cqrs-ddd](.claude/skills/cqrs-ddd/SKILL.md) | レイヤー依存関係、集約設計、Command/Query Handler、Entity Configuration |
| 🧪 [testing-strategy](.claude/skills/testing-strategy/SKILL.md) | Application層統合テスト、テストビルダーパターン |
| 🔍 [slow-query-detector](.claude/skills/slow-query-detector/SKILL.md) | N+1問題検出、クエリ最適化 |

## 命名規則

| 対象 | パターン | 例 |
| ---- | -------- | --- |
| 集約フォルダ | `{Name}Aggregate/` | `EnrollmentAggregate/` |
| Command/Query | `{動詞}{名詞}Command/Query` | `EnrollStudentCommand` |
| DTO | `{用途}Dto` | `EnrollmentSummaryDto` |
| 値オブジェクト | 単数形 | `StudentId`, `CourseCode` |

## 新規Aggregate実装チェックリスト

1. **Domain層**
   - [ ] `{Name}Aggregate/` フォルダ作成
   - [ ] エンティティ: `{Name}.cs`
   - [ ] 値オブジェクト: `{Name}Id.cs`
   - [ ] リポジトリIF: `I{Name}Repository.cs`

2. **Infrastructure層** ← **よく忘れられる**
   - [ ] Entity Configuration: `{Name}Configuration.cs`
   - [ ] DbContext に `DbSet<{Name}>` 追加
   - [ ] マイグレーション: `Vn__{Name}_Table.sql`
   - [ ] リポジトリ実装: `{Name}Repository.cs`

3. **Application層**
   - [ ] Command/Query + Handler

4. **API層**
   - [ ] Controller + DI設定

5. **テスト**
   - [ ] Application層統合テスト

## 主要コマンド

```bash
# 環境管理
npm run up          # Docker環境起動
npm run down        # 停止
npm run rebuild     # リビルド+起動
npm run clean       # 停止+DBリセット

# 開発
npm run build       # ビルド
npm run test:unit   # テスト実行
npm run format      # コードフォーマット
npm run swagger     # Swagger UI表示

# ログ
npm run logs:api    # APIログ
npm run logs:migrate # マイグレーションログ
```

詳細なコマンドは [README.md](README.md) を参照。

## コンテキストマップ

### StudentRegistrations ←→ Enrollments

**関係性:** Customer-Supplier（Enrollments が Customer）

**統合方式:** ACL (Anti-Corruption Layer) - HTTP通信

```
Enrollments.EnrollStudentCommandHandler
  → IStudentServiceClient (ACL)
    → HTTP GET /api/students/{id}
      → StudentRegistrations API
```

**Shared Kernel:** `StudentId` 値オブジェクト

**スキーマ分離:**

- `student_registrations`: students テーブル
- `courses`: courses, semesters, course_offerings, enrollments テーブル

詳細は [contexts/CONTEXT_MAP.md](contexts/CONTEXT_MAP.md) を参照。

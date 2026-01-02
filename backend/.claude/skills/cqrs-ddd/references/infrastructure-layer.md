# Infrastructure層 実装パターン

## DbContext（Unit of Workパターン）

Entity Framework Coreの `DbContext` がUnit of Workとして機能し、トランザクション管理を担当。

```csharp
public class EnrollmentDbContext : DbContext
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();

    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("enrollment");

        // 集約ごとに設定を分離
        modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // ドメインイベント発行処理を追加可能
        await DispatchDomainEventsAsync();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker
            .Entries<AggregateRoot<object>>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            // MediatRやメッセージバスで発行
            // await _mediator.Publish(domainEvent);
        }
    }
}
```

---

## Entity Configuration（Fluent API）

集約ごとに `IEntityTypeConfiguration<T>` を実装し、マッピング設定を分離。

### 基本的な設定例

```csharp
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        // テーブル設定
        builder.ToTable("Enrollments");
        builder.HasKey(e => e.Id);

        // 主キー（値オブジェクト）の変換
        builder.Property(e => e.Id)
            .HasConversion(
                v => v.Value,
                v => new EnrollmentId(v))
            .HasColumnName("Id");

        // 外部キー（値オブジェクト）
        builder.Property(e => e.StudentId)
            .HasConversion(
                v => v.Value,
                v => new StudentId(v))
            .IsRequired();

        builder.Property(e => e.CourseCode)
            .HasConversion(
                v => v.Value,
                v => new CourseCode(v))
            .HasMaxLength(10)
            .IsRequired();

        // 複合値オブジェクト（OwnsOne）
        builder.OwnsOne(e => e.Semester, semester =>
        {
            semester.Property(s => s.Year)
                .HasColumnName("SemesterYear")
                .IsRequired();

            semester.Property(s => s.Period)
                .HasConversion<string>()
                .HasColumnName("SemesterPeriod")
                .HasMaxLength(10)
                .IsRequired();
        });

        // Enum（文字列として保存）
        builder.Property(e => e.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<EnrollmentStatus>(v))
            .HasMaxLength(20)
            .IsRequired();

        // インデックス
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.CourseCode);
        builder.HasIndex(e => new { e.StudentId, e.SemesterYear, e.SemesterPeriod });

        // ドメインイベントは永続化しない
        builder.Ignore(e => e.DomainEvents);
    }
}
```

### リレーションシップ設定（集約間参照は禁止）

```csharp
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(v => v.Value, v => new StudentId(v));

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(s => s.Email).IsUnique();

        // 集約内のエンティティコレクション（所有している場合のみ）
        // builder.OwnsMany(s => s.ContactInfo, ...);
    }
}
```

---

## リポジトリ実装

集約ルートごとにリポジトリを実装。基本的なCRUD操作を提供。

```csharp
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly EnrollmentDbContext _context;

    public EnrollmentRepository(EnrollmentDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(EnrollmentId id)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Enrollment>> GetByStudentIdAsync(
        StudentId studentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Enrollment>> GetByCourseCodeAsync(
        CourseCode courseCode,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Where(e => e.CourseCode == courseCode)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Enrollment enrollment)
    {
        await _context.Enrollments.AddAsync(enrollment);
        // SaveChangesはServiceで呼ぶ（Unit of Workパターン）
    }

    public Task UpdateAsync(Enrollment enrollment)
    {
        _context.Enrollments.Update(enrollment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EnrollmentId id)
    {
        var enrollment = _context.Enrollments.Find(id);
        if (enrollment != null)
        {
            _context.Enrollments.Remove(enrollment);
        }
        return Task.CompletedTask;
    }
}
```

### リポジトリ設計原則

- **SaveChangesは呼ばない**: DbContextのUnit of Work機能に任せる
- **シンプルな取得メソッドのみ**: 複雑なクエリはQueryHandlerで実装
- **集約全体を取得**: 必要なら `Include()` で関連エンティティも取得
- **IQueryable は返さない**: リポジトリ外にクエリロジックが漏れるのを防ぐ
- **命名規約**:
  - 単一エンティティ取得: `Get*Async` (例: `GetByIdAsync`, `GetByCodeAsync`)
  - 複数エンティティ取得: `Select*Async` (例: `SelectBySemesterAsync`, `SelectByStatusAsync`)

---

## 依存性注入の設定

### Program.cs / Startup.cs

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddEnrollmentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext登録
        services.AddDbContext<EnrollmentDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("EnrollmentDb"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(
                        typeof(EnrollmentDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                }));

        // リポジトリ登録
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();

        // ドメインサービス登録
        services.AddScoped<EnrollmentDomainService>();

        return services;
    }
}
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "EnrollmentDb": "Host=localhost;Database=university_enrollment;Username=postgres;Password=password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## マイグレーション（Flyway）

このプロジェクトではデータベーススキーマ管理に **Flyway** を使用します。

### ディレクトリ構造

```
Infrastructure/
└── Persistence/
    └── Migrations/
        ├── V1__Initial_Schema.sql
        ├── V2__Add_Enrollment_Indexes.sql
        └── V3__Add_Student_Email_Unique.sql
```

### マイグレーションファイルの命名規則

```
V{バージョン}__{説明}.sql

例:
V1__Initial_Schema.sql
V2__Add_Enrollment_Indexes.sql
V2.1__Fix_CourseCode_Length.sql
```

### マイグレーション例（V1__Initial_Schema.sql）

```sql
-- Enrollmentスキーマ作成
CREATE SCHEMA IF NOT EXISTS enrollment;

-- Enrollmentsテーブル
CREATE TABLE enrollment.enrollments (
    id UUID PRIMARY KEY,
    student_id UUID NOT NULL,
    course_code VARCHAR(10) NOT NULL,
    semester_year INT NOT NULL,
    semester_period VARCHAR(10) NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- インデックス
CREATE INDEX idx_enrollments_student_id
    ON enrollment.enrollments(student_id);

CREATE INDEX idx_enrollments_course_code
    ON enrollment.enrollments(course_code);

CREATE INDEX idx_enrollments_semester
    ON enrollment.enrollments(student_id, semester_year, semester_period);

-- Studentsテーブル
CREATE TABLE enrollment.students (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    max_enrollments INT NOT NULL DEFAULT 20,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Coursesテーブル
CREATE TABLE enrollment.courses (
    code VARCHAR(10) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    credits INT NOT NULL,
    max_capacity INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### Flyway実行コマンド

```bash
# マイグレーション情報確認
flyway info -configFiles=flyway.conf

# マイグレーション実行
flyway migrate -configFiles=flyway.conf

# マイグレーション検証
flyway validate -configFiles=flyway.conf

# 最新マイグレーションのロールバック（Flywayコマーシャル版のみ）
flyway undo -configFiles=flyway.conf

# マイグレーション履歴クリア（開発環境のみ）
flyway clean -configFiles=flyway.conf
```

---

## 新規Aggregate追加時の完全なプロセス

**⚠️ よくある忘れ：** 新しい集約を追加する際に、マイグレーション生成を忘れることがあります。
このセクションでは、新規Aggregateを追加する際の **必須手順** をまとめます。

### ステップ1: マイグレーションファイルの作成

新しいテーブルが必要な場合、**必ず最初にマイグレーションファイルを作成**してください。

```bash
# 現在のマイグレーションバージョン確認
ls -la src/Enrollments/Infrastructure/Persistence/Migrations/

# 出力例:
# V1__Initial_Schema.sql
# V2__Add_Student_Table.sql
# → 次は V3 を作成

# 例: Enrollment テーブルを追加する場合
cat > src/Enrollments/Infrastructure/Persistence/Migrations/V3__Add_Enrollment_Table.sql << 'EOF'
-- Enrollmentテーブル追加
CREATE TABLE courses.enrollments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL,
    course_code VARCHAR(10) NOT NULL,
    semester_year INT NOT NULL,
    semester_period VARCHAR(10) NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- インデックス
CREATE INDEX idx_enrollments_student_id ON courses.enrollments(student_id);
CREATE INDEX idx_enrollments_course_code ON courses.enrollments(course_code);
CREATE INDEX idx_enrollments_semester
    ON courses.enrollments(student_id, semester_year, semester_period);

-- 外部キー制約
ALTER TABLE courses.enrollments
ADD CONSTRAINT fk_enrollments_student_id
    FOREIGN KEY (student_id) REFERENCES courses.students(id);

ALTER TABLE courses.enrollments
ADD CONSTRAINT fk_enrollments_course_code
    FOREIGN KEY (course_code) REFERENCES courses.courses(code);
EOF
```

### ステップ2: DbContext に DbSet を追加

`CoursesDbContext.cs` に新しい集約の DbSet を追加します。

```csharp
public class CoursesDbContext : DbContext
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();  // ← 追加

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("courses");

        // 既存の設定
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
        modelBuilder.ApplyConfiguration(new StudentConfiguration());

        // 新規追加
        modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());  // ← 追加
    }
}
```

### ステップ3: Entity Configuration (Fluent API) を作成

`Persistence/Configuration/` ディレクトリに Entity Configuration を作成します。

```csharp
// Persistence/Configuration/EnrollmentConfiguration.cs
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments", "courses");
        builder.HasKey(e => e.Id);

        // 主キー（値オブジェクト）の変換
        builder.Property(e => e.Id)
            .HasConversion(
                v => v.Value,
                v => new EnrollmentId(v))
            .HasColumnName("id");

        // 外部キー（値オブジェクト）
        builder.Property(e => e.StudentId)
            .HasConversion(
                v => v.Value,
                v => new StudentId(v))
            .HasColumnName("student_id")
            .IsRequired();

        builder.Property(e => e.CourseCode)
            .HasConversion(
                v => v.Value,
                v => new CourseCode(v))
            .HasColumnName("course_code")
            .HasMaxLength(10)
            .IsRequired();

        // 複合値オブジェクト
        builder.OwnsOne(e => e.Semester, semester =>
        {
            semester.Property(s => s.Year)
                .HasColumnName("semester_year");
            semester.Property(s => s.Period)
                .HasConversion<string>()
                .HasColumnName("semester_period")
                .HasMaxLength(10);
        });

        // Enum
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);

        // タイムスタンプ
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // インデックス
        builder.HasIndex(e => e.StudentId).HasName("idx_enrollments_student_id");
        builder.HasIndex(e => e.CourseCode).HasName("idx_enrollments_course_code");
        builder.HasIndex(e => new { e.StudentId, SemesterYear = e.Semester.Year, SemesterPeriod = e.Semester.Period })
            .HasName("idx_enrollments_semester");

        // ドメインイベントは永続化しない
        builder.Ignore(e => e.DomainEvents);
    }
}
```

### ステップ4: リポジトリを実装

`Persistence/Repositories/EnrollmentRepository.cs` を作成します。

```csharp
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly CoursesDbContext _context;

    public EnrollmentRepository(CoursesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Enrollment enrollment)
    {
        await _context.Enrollments.AddAsync(enrollment);
    }

    public async Task<Enrollment?> GetByIdAsync(EnrollmentId id)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Enrollment>> GetByStudentIdAsync(StudentId studentId)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .ToListAsync();
    }

    public Task UpdateAsync(Enrollment enrollment)
    {
        _context.Enrollments.Update(enrollment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EnrollmentId id)
    {
        var enrollment = _context.Enrollments.Find(id);
        if (enrollment != null)
        {
            _context.Enrollments.Remove(enrollment);
        }
        return Task.CompletedTask;
    }
}
```

### ステップ5: 依存性注入を設定

`Program.cs` にリポジトリを登録します。

```csharp
// Program.cs
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
```

### ステップ6: Docker環境をリセットして検証

マイグレーション適用と動作確認を行います。

```bash
cd backend

# 環境をクリーンにリセット
make clean

# 新規マイグレーションを適用して起動
make up

# マイグレーション実行ログ確認
docker-compose logs flyway | tail -20
```

---

## よくある忘れと対策

| 忘れやすい項目 | 影響 | 対策 |
| ------------- | ---- | ---- |
| ❌ マイグレーションファイルを作成しない | テーブルが DB に作成されない → 500エラー | **ステップ1を最初に実行** |
| ❌ DbContext に DbSet を追加しない | EF がテーブルを認識しない → 実行時エラー | **ステップ2を忘れずに** |
| ❌ Entity Configuration を作成しない | リポジトリがテーブルを見つけられない | **ステップ3で Fluent API 設定** |
| ❌ リポジトリの依存性注入を忘れる | DI が失敗 → 起動時エラー | **ステップ5で Program.cs に登録** |

---

### flyway.conf 設定例

```properties
# Database connection
flyway.url=jdbc:postgresql://localhost:5432/university_enrollment
flyway.user=postgres
flyway.password=password

# Migration settings
flyway.locations=filesystem:src/Enrollments/Infrastructure/Persistence/Migrations
flyway.schemas=enrollment
flyway.table=flyway_schema_history
flyway.baselineOnMigrate=true
flyway.validateOnMigrate=true

# Placeholder substitution
flyway.placeholderReplacement=true
flyway.placeholders.schema=enrollment
```

### DbContext設定（Flywayでマイグレーション管理）

```csharp
public class EnrollmentDbContext : DbContext
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();

    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("enrollment");

        // Entity Configurationを適用
        modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new CourseConfiguration());

        // マイグレーションはFlywayで管理するため、EF Coreのマイグレーション機能は無効化
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // EF Core マイグレーション機能を無効化
        optionsBuilder.UseMigrationsAssembly(null);
    }
}
```

### CI/CDでのFlyway実行例

```yaml
# GitHub Actions例
- name: Run Flyway Migrations
  run: |
    flyway migrate \
      -url=jdbc:postgresql://${{ secrets.DB_HOST }}:5432/${{ secrets.DB_NAME }} \
      -user=${{ secrets.DB_USER }} \
      -password=${{ secrets.DB_PASSWORD }} \
      -locations=filesystem:src/Enrollments/Infrastructure/Persistence/Migrations \
      -schemas=enrollment
```

---

## 外部サービス統合

### インターフェース定義（Domain層）

```csharp
public interface IEmailService
{
    Task SendEnrollmentConfirmationAsync(
        string recipientEmail,
        EnrollmentId enrollmentId,
        CourseCode courseCode);
}
```

### 実装（Infrastructure層）

```csharp
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IConfiguration configuration,
        ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEnrollmentConfirmationAsync(
        string recipientEmail,
        EnrollmentId enrollmentId,
        CourseCode courseCode)
    {
        try
        {
            using var client = new SmtpClient(_configuration["Smtp:Host"]);
            var message = new MailMessage
            {
                From = new MailAddress(_configuration["Smtp:From"]),
                Subject = "履修登録完了のお知らせ",
                Body = $"科目 {courseCode.Value} の履修登録が完了しました。"
            };
            message.To.Add(recipientEmail);

            await client.SendMailAsync(message);

            _logger.LogInformation(
                "Enrollment confirmation email sent to {Email} for {EnrollmentId}",
                recipientEmail, enrollmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send enrollment confirmation email to {Email}",
                recipientEmail);
            // 例外を再スローするかは要件次第
            // throw;
        }
    }
}
```

---

## Query最適化パターン

### Dapper使用例（複雑な読み取り専用クエリ）

```csharp
public class GetEnrollmentStatisticsQueryHandler
    : IRequestHandler<GetEnrollmentStatisticsQuery, EnrollmentStatisticsDto>
{
    private readonly IDbConnection _connection;

    public GetEnrollmentStatisticsQueryHandler(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<EnrollmentStatisticsDto> Handle(
        GetEnrollmentStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                COUNT(*) as TotalEnrollments,
                COUNT(CASE WHEN Status = 'Approved' THEN 1 END) as ApprovedCount,
                COUNT(CASE WHEN Status = 'Pending' THEN 1 END) as PendingCount,
                COUNT(DISTINCT StudentId) as UniqueStudents,
                COUNT(DISTINCT CourseCode) as UniqueCourses
            FROM enrollment.Enrollments
            WHERE SemesterYear = @Year AND SemesterPeriod = @Period";

        return await _connection.QueryFirstAsync<EnrollmentStatisticsDto>(
            sql,
            new { Year = query.Year, Period = query.Period });
    }
}
```

---

## ベストプラクティス

1. **DbContext設計**
   - 境界づけられたコンテキストごとに別DbContext
   - SaveChangesAsync()でドメインイベント発行
   - 接続文字列は環境変数で管理

2. **マッピング戦略**
   - 値オブジェクト: `HasConversion()`
   - 複合値オブジェクト: `OwnsOne()`
   - Enum: 文字列として保存（将来の拡張性）

3. **パフォーマンス**
   - 適切なインデックス設計
   - N+1問題の回避（Include/AsNoTracking）
   - 読み取り専用クエリはDapperも検討

4. **トランザクション**
   - 基本はDbContextの暗黙的トランザクション
   - 複雑な場合のみBeginTransaction()
   - リトライロジックを実装（一時的障害対策）

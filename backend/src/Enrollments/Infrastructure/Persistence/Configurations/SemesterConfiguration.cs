using Enrollments.Domain.SemesterAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enrollments.Infrastructure.Persistence.Configurations;

public class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    public void Configure(EntityTypeBuilder<Semester> builder)
    {
        builder.ToTable("semesters");

        // バッキングフィールドを使用して複合キーをマッピング
        builder.Property("_idYear")
            .HasColumnName("id_year")
            .IsRequired();

        builder.Property("_idPeriod")
            .HasColumnName("id_period")
            .HasMaxLength(20)
            .IsRequired();

        // 複合主キー定義
        builder.HasKey("_idYear", "_idPeriod");

        // Idプロパティ - 読み取り専用としてマッピング（バッキングフィールドから計算）
        builder.Ignore(s => s.Id);

        // その他のプロパティ
        // DateTime型をUTCとして扱う（PostgreSQL timestamp with time zone 対応）
        builder.Property(s => s.StartDate)
            .HasColumnName("start_date")
            .HasConversion(
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc),  // DBへ保存時
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))  // DBから読み込み時
            .IsRequired();

        builder.Property(s => s.EndDate)
            .HasColumnName("end_date")
            .HasConversion(
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
            .IsRequired();

        // ドメインイベントは永続化しない
        builder.Ignore(s => s.DomainEvents);
    }
}
